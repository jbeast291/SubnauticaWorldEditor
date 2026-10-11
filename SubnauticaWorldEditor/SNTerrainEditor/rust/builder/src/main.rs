use std::{collections::HashSet, ffi::OsStr, fmt, fs, iter, io, path::Path, process, time};
use anyhow::{Context, anyhow, bail};

fn main() -> process::ExitCode {
    match run(clap::Parser::parse()) {
        Ok(()) => process::ExitCode::SUCCESS,
        Err(e) => {
            printerr!("{e:?}");
            process::ExitCode::FAILURE
        },
    }
}

/// Command-line tool to build the project for multiple target platforms and CPUs
#[derive(clap::Parser)]
struct Cli {
    /// Prevent changes to the lock file
    #[arg(long)]
    ci: bool,

    /// Builder operation to perform
    #[command(subcommand)]
    command: Command,
}

#[derive(clap::Subcommand)]
enum Command {
    /// Build the project for all target platforms and CPUs
    Build {
        /// Faster build with debugging features for the current platform and native CPU only
        #[arg(long, conflicts_with = "pgo")]
        dev: bool,

        /// Build with profile-guided optimizations for supported target platforms and CPUs
        #[arg(long)]
        pgo: bool,
    },

    /// Test the project on the host platform for each supported CPU
    Test { },

    /// Benchmark the project on the host platform for each supported CPU
    Bench {
        /// Build with profile-guided optimizations
        #[arg(long)]
        pgo: bool,

        /// Compare performance between target CPUs instead of against a baseline
        #[arg(long, conflicts_with_all = ["cmp", "base"])]
        cpus: bool,

        /// Compare performance against the current baseline without saving
        #[arg(long)]
        cmp: bool,

        /// Save and compare performance against a named baseline
        #[arg(long, value_parser = parse_criterion_arg)]
        base: Option<String>,

        /// Filter benchmarks to run
        #[arg(value_parser = parse_criterion_arg)]
        filter: Option<String>,
    },
}

fn parse_criterion_arg(arg: &str) -> anyhow::Result<String> {
    if arg.starts_with('-') { bail!("value must not begin with a hyphen") }
    else { Ok(arg.to_owned()) }
}

fn run(args: Cli) -> anyhow::Result<()> {
    let timer = time::Instant::now();

    let Cli { ci, command } = args;
    match command {
        Command::Build { dev: false, pgo: false } => build(ci)?,
        Command::Build { dev: true, pgo: false } => build_dev(ci)?,
        Command::Build { dev: false, pgo: true } => build_pgo(ci)?,
        Command::Build { dev: true, pgo: true } => unreachable!(),
        Command::Test { } => test(ci)?,
        Command::Bench { pgo: false, cpus: false, cmp, base, filter }
            => bench(ci, Baseline::new(cmp, base.as_deref()), filter.as_deref())?,
        Command::Bench { pgo: false, cpus: true, cmp: false, base: None, filter }
            => bench(ci, Baseline::cpus(), filter.as_deref())?,
        Command::Bench { pgo: true, cpus: false, cmp, base, filter }
            => bench_pgo(ci, Baseline::new(cmp, base.as_deref()), filter.as_deref())?,
        Command::Bench { pgo: true, cpus: true, cmp: false, base: None, filter }
            => bench_pgo(ci, Baseline::cpus(), filter.as_deref())?,
        Command::Bench { cpus: true, cmp: true, .. } |
        Command::Bench { cpus: true, base: Some(_), .. } => unreachable!(),
    }

    let elapsed = timer.elapsed().as_secs_f32();
    printinfo!("all done in {elapsed:.2}s");

    Ok(())
}

fn build(ci: bool) -> anyhow::Result<()> {
    fs::create_dir_all("target/builder").err_path("target/builder")?;

    let platforms = Target::platforms();
    for &target in platforms { target.check(); }

    let mut outputs = HashSet::new();
    for &target in platforms {
        let cpus = target.cpus();
        for &cpu in &cpus {
            cdylib_build(ci, false, &mut outputs, target, cpu)
                .with_context(|| format!("building cdylib for {target} on {cpu}"))?;
        }

        dispatch_build(ci, false, &mut outputs, target, &cpus)
            .with_context(|| format!("building dispatch for {target}"))?;
    }

    clean_dir("target/builder", |n| n.to_str().is_some_and(|n| outputs.contains(n)))
        .context("cleaning output dir")?;

    printinfo!("output is in: target/builder");
    Ok(())
}

fn build_dev(ci: bool) -> anyhow::Result<()> {
    fs::create_dir_all("target/builder").err_path("target/builder")?;

    let target = Target::current().context("unable to determine target for dev build")?;
    target.check();
    let cpu = Cpu::Native;

    let mut outputs = HashSet::new();
    cdylib_build(ci, true, &mut outputs, target, cpu)
        .with_context(|| format!("building cdylib for {target} on {cpu}"))?;

    dispatch_build(ci, true, &mut outputs, target, &[cpu])
        .with_context(|| format!("building dispatch for {target}"))?;

    clean_dir("target/builder", |n| n.to_str().is_some_and(|n| outputs.contains(n)))
        .context("cleaning output dir")?;

    printinfo!("output is in: target/builder");
    Ok(())
}

fn cdylib_build(
    ci: bool, dev: bool, outputs: &mut HashSet<String>, target: Target, cpu: Cpu,
) -> anyhow::Result<()> {
    printinfo!("building cdylib for {target} on {cpu}");

    let mut cmd = process::Command::new("cargo");
    cmd.env("RUSTFLAGS", format!("-C target-cpu={}", cpu.rustflag_name()));
    cmd.arg("+stable");
    cmd.args(target.cargo_build_tool().args());
    cmd.args([
        "--package", "optoctrees-cdylib",
        "--profile", if dev { "dev" } else { "release" },
        "--target", target.cli_tuple(),
    ]);
    if ci { cmd.arg("--locked"); }
    cmd.run()?;

    let out_tuple = target.out_tuple();
    let profile_dir = if dev { "debug" } else { "release" };
    let (prefix, suffix) = (target.cdylib_prefix(), target.cdylib_suffix());
    let src = format!("target/{out_tuple}/{profile_dir}/{prefix}optoctrees{suffix}");

    let name = format!("{prefix}optoctrees-{cpu}{suffix}");
    if !outputs.insert(name.clone()) {
        bail!("multiple builds produced output with file name: {name}")
    }

    let dst = format!("target/builder/{name}");
    fs::copy(&src, &dst).with_context(|| format!("copying '{src}' to '{dst}'"))?;

    Ok(())
}

fn dispatch_build(
    ci: bool, dev: bool, outputs: &mut HashSet<String>, target: Target, cpus: &[Cpu],
) -> anyhow::Result<()> {
    printinfo!("building dispatch for {target}");

    let features = cpus.iter()
        .map(|cpu| format!("cpu-{cpu}"))
        .zip(iter::repeat(",".to_owned()))
        .map(|(a, b)| [a, b])
        .flatten()
        .collect::<String>();

    let mut cmd = process::Command::new("cargo");
    cmd.env("RUSTFLAGS", "");
    cmd.arg("+stable");
    cmd.args(target.cargo_build_tool().args());
    cmd.args([
        "--package", "optoctrees-dispatch",
        "--profile", if dev { "dev" } else { "release" },
        "--target", target.cli_tuple(),
        "--features", &features,
    ]);
    if ci { cmd.arg("--locked"); }
    cmd.run()?;

    let out_tuple = target.out_tuple();
    let profile_dir = if dev { "debug" } else { "release" };
    let (prefix, suffix) = (target.cdylib_prefix(), target.cdylib_suffix());
    let src = format!("target/{out_tuple}/{profile_dir}/{prefix}optoctrees_dispatch{suffix}");

    let name = format!("{prefix}optoctrees{suffix}");
    if !outputs.insert(name.clone()) {
        bail!("multiple builds produced output with file name: {name}")
    }

    let dst = format!("target/builder/{name}");
    fs::copy(&src, &dst).with_context(|| format!("copying '{src}' to '{dst}'"))?;

    Ok(())
}

fn test(ci: bool) -> anyhow::Result<()> {
    let target = Target::Host;
    let cpus = target.cpus();

    for &cpu in &cpus {
        rlib_test(ci, target, cpu)
            .with_context(|| format!("testing rlib for {target} on {cpu}"))?;
    }

    if !cpus.contains(&Cpu::X64V1) { printwarn!("unable to test on x86-64-v1 using native cpu"); }
    if !cpus.contains(&Cpu::X64V2) { printwarn!("unable to test on x86-64-v2 using native cpu"); }
    if !cpus.contains(&Cpu::X64V3) { printwarn!("unable to test on x86-64-v3 using native cpu"); }
    if !cpus.contains(&Cpu::X64V4) { printwarn!("unable to test on x86-64-v4 using native cpu"); }

    Ok(())
}

fn rlib_test(ci: bool, target: Target, cpu: Cpu) -> anyhow::Result<()> {
    printinfo!("testing rlib for {target} on {cpu}");

    let mut cmd = process::Command::new("cargo");
    cmd.env("RUSTFLAGS", format!("-C target-cpu={}", cpu.rustflag_name()));
    cmd.args([
        "+stable", "test",
        "--package", "optoctrees",
        "--target", target.cli_tuple(),
    ]);
    if ci { cmd.arg("--locked"); }
    cmd.run()
}

fn bench(ci: bool, base: Baseline, filter: Option<&str>) -> anyhow::Result<()> {
    if ci { printwarn!("benching in ci is not recommended"); }

    let target = Target::Host;
    let cpus = target.cpus();

    if let Some(wipe_dir) = base.wipe_dir() {
        recursive_wipe_dir_anchored("target/criterion".as_ref(), wipe_dir, 3)
            .context("wiping previous bench baseline")?;
    }

    for &cpu in &cpus {
        rlib_bench(ci, &base, filter, target, cpu)
            .with_context(|| format!("benching rlib for {target} on {cpu}"))?;
    }

    if !cpus.contains(&Cpu::X64V1) { printwarn!("unable to bench on x86-64-v1 using native cpu"); }
    if !cpus.contains(&Cpu::X64V2) { printwarn!("unable to bench on x86-64-v2 using native cpu"); }
    if !cpus.contains(&Cpu::X64V3) { printwarn!("unable to bench on x86-64-v3 using native cpu"); }
    if !cpus.contains(&Cpu::X64V4) { printwarn!("unable to bench on x86-64-v4 using native cpu"); }

    Ok(())
}

fn rlib_bench(
    ci: bool, base: &Baseline, filter: Option<&str>, target: Target, cpu: Cpu,
) -> anyhow::Result<()> {
    printinfo!("benching rlib for {target} on {cpu}");

    let mut cmd = process::Command::new("cargo");
    cmd.env("RUSTFLAGS", format!("-C target-cpu={}", cpu.rustflag_name()));
    cmd.args([
        "+stable", "bench",
        "--package", "optoctrees",
        "--target", target.cli_tuple(),
        "--",
    ]);
    cmd.args(base.criterion_args(cpu));
    if let Some(filter) = filter { cmd.arg(filter); }
    if ci { cmd.arg("--locked"); }
    cmd.run()
}

fn build_pgo(#[expect(unused)] ci: bool) -> anyhow::Result<()> {
    fs::create_dir_all("target/builder").err_path("target/builder")?;

    bail!("profile-guided optimization is not yet implemented")
}

fn bench_pgo(
    #[expect(unused)] ci: bool,
    #[expect(unused)] base: Baseline,
    #[expect(unused)] filter: Option<&str>,
) -> anyhow::Result<()> {
    bail!("profile-guided optimization is not yet implemented")
}

fn clean_dir(
    dir: &(impl AsRef<Path> + ?Sized),
    mut retain: impl FnMut(&OsStr) -> bool,
) -> anyhow::Result<()> {
    let dir = dir.as_ref();
    for entry in fs::read_dir(dir).err_path(dir)? {
        let entry = entry.err_path(dir)?;
        if retain(&entry.file_name()) { continue }

        let path = entry.path();
        let ty = entry.file_type().err_path(&path)?;
        if ty.is_file() || ty.is_symlink() {
            fs::remove_file(&path).err_path(&path)?;
        } else if ty.is_dir() {
            fs::remove_dir_all(&path).err_path(&path)?;
        }
    }
    Ok(())
}

fn recursive_wipe_dir_anchored(dir: &Path, wipe: &str, depth: usize) -> anyhow::Result<()> {
    let (needle, rm) = wipe
        .split_once('/')
        .map(|(needle, rm)| (needle, Some(rm)))
        .unwrap_or((wipe, None));

    fn recurse(dir: &Path, needle: &str, rm: Option<&str>, depth: usize) -> anyhow::Result<()> {
        let entries = match fs::read_dir(dir) {
            Ok(entries) => entries,
            Err(e) if e.kind() == io::ErrorKind::NotFound => return Ok(()),
            Err(e) => return Err(e).err_path(dir),
        };

        for entry in entries {
            let entry = entry.err_path(dir)?;
            let path = entry.path();
            if !entry.file_type().err_path(&path)?.is_dir() { continue }

            if entry.file_name() == needle {
                if let Some(rm) = rm {
                    let target = path.join(rm);
                    match fs::remove_dir_all(&target) {
                        Ok(()) => (),
                        Err(e) if e.kind() == io::ErrorKind::NotFound => (),
                        Err(e) => return Err(e).err_path(&target),
                    }
                } else {
                    fs::remove_dir_all(&path).err_path(&path)?;
                }
            } else if let Some(depth) = depth.checked_sub(1) {
                recurse(&path, needle, rm, depth)?;
            }
        }

        Ok(())
    }

    recurse(dir, needle, rm, depth)
}

enum Baseline<'b> {
    Cpus,
    Baseline { cmp: bool, base: &'b str },
}

impl<'b> Baseline<'b> {
    fn new(cmp: bool, base: Option<&'b str>) -> Self {
        let base = base.unwrap_or("base");
        Baseline::Baseline { cmp, base }
    }

    fn cpus() -> Self { Baseline::Cpus }

    fn criterion_args(&self, cpu: Cpu) -> impl IntoIterator<Item = String> {
        match self {
            Baseline::Cpus => ["--save-baseline".to_owned(), "builder/cpus".to_owned()],
            Baseline::Baseline { cmp, base } => {
                let flag = if *cmp { "--baseline-lenient" } else { "--save-baseline" };
                let arg = format!("builder/cpu-{cpu}/{base}");
                [flag.to_owned(), arg]
            },
        }
    }

    fn wipe_dir(&self) -> Option<&'static str> {
        match self {
            Baseline::Cpus => Some("builder/cpus"),
            Baseline::Baseline { .. } => None,
        }
    }
}

#[derive(Copy, Clone)]
enum Target { Host, #[expect(unused)] Linux, Windows, Macos }

impl Target {
    fn platforms() -> &'static [Self] {
        &[Target::Windows, Target::Macos]
    }

    fn current() -> anyhow::Result<Self> {
        cfg_select! {
            target_os = "linux" => Ok(Target::Windows),
            target_os = "windows" => Ok(Target::Windows),
            target_os = "macos" => Ok(Target::Macos),
            _ => bail!("unrecognized host operating system"),
        }
    }

    fn name(self) -> &'static str {
        match self {
            Target::Host => "host",
            Target::Linux => "linux",
            Target::Windows => "windows",
            Target::Macos => "macos",
        }
    }

    fn cargo_build_tool(self) -> CargoTool {
        match self {
            Target::Host => CargoTool::Build,
            Target::Linux => CargoTool::Zigbuild,
            Target::Windows => CargoTool::Build,
            Target::Macos => cfg_select! {
                target_os = "macos" => CargoTool::Build,
                _ => CargoTool::Zigbuild,
            },
        }
    }

    fn check(self) {
        self.ensure_rustup_installed().handle_missing_dep();
        self.ensure_deps_installed().handle_missing_dep();
        self.cargo_build_tool().ensure_installed().handle_missing_dep();
    }

    fn ensure_rustup_installed(self) -> Result<(), MissingDependency> {
        let Some(tup) = self.rustup_tuple() else { return Ok(()) };

        let map_err = |err| {
            let name = format!("rustup target {tup}");
            let cmd = format!("rustup target add --toolchain stable {tup}");
            if let Some(doc) = self.rustc_doc_url() {
                MissingDependency::new(name, [cmd, doc.to_owned()], err)
            } else {
                MissingDependency::new(name, [cmd], err)
            }
        };

        let targets = process::Command::new("rustup")
            .args(["target", "list", "--toolchain", "stable", "--installed"])
            .eval()
            .map_err(map_err)?;

        for target in targets.lines() {
            if target == tup { return Ok(()) }
        }

        Err(map_err(anyhow!("target not found in rustc output")))
    }

    fn ensure_deps_installed(self) -> Result<(), MissingDependency> {
        match self {
            Target::Host | Target::Linux | Target::Macos => Ok(()),
            Target::Windows => process::Command::new("x86_64-w64-mingw32-clang").arg("--version")
                .quiet().run()
                .err_missing_dep("llvm-mingw", ["https://github.com/mstorsjo/llvm-mingw"]),
        }
    }

    fn rustup_tuple(self) -> Option<&'static str> {
        match self {
            Target::Host => None,
            Target::Linux => Some("x86_64-unknown-linux-gnu"),
            Target::Windows => Some("x86_64-pc-windows-gnullvm"),
            Target::Macos => Some("x86_64-apple-darwin"),
        }
    }

    fn rustc_doc_url(self) -> Option<&'static str> {
        match self {
            Target::Host | Target::Linux => None,
            Target::Windows => Some(
                "https://doc.rust-lang.org/rustc/platform-support/windows-gnullvm.html",
            ),
            Target::Macos => Some(
                "https://doc.rust-lang.org/rustc/platform-support/apple-darwin.html",
            ),
        }
    }

    fn cli_tuple(self) -> &'static str {
        match self {
            Target::Host => "host-tuple",
            Target::Linux => "x86_64-unknown-linux-gnu.2.17",
            Target::Windows => "x86_64-pc-windows-gnullvm",
            Target::Macos => "x86_64-apple-darwin",
        }
    }

    fn out_tuple(self) -> &'static str {
        match self {
            Target::Host => current_platform::CURRENT_PLATFORM,
            Target::Linux => "x86_64-unknown-linux-gnu",
            Target::Windows => "x86_64-pc-windows-gnullvm",
            Target::Macos => "x86_64-apple-darwin",
        }
    }

    fn cdylib_prefix(self) -> &'static str {
        match self {
            Target::Host => std::env::consts::DLL_PREFIX,
            Target::Linux => "lib",
            Target::Windows => "",
            Target::Macos => "lib",
        }
    }

    fn cdylib_suffix(self) -> &'static str {
        match self {
            Target::Host => std::env::consts::DLL_SUFFIX,
            Target::Linux => ".so",
            Target::Windows => ".dll",
            Target::Macos => ".dylib",
        }
    }

    fn cpus(self) -> Vec<Cpu> {
        match self {
            Target::Host => Cpu::current(),
            Target::Linux | Target::Windows | Target::Macos
                => vec![Cpu::X64V1, Cpu::X64V2, Cpu::X64V3, Cpu::X64V4],
        }
    }
}

impl fmt::Display for Target {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result { fmt::Display::fmt(self.name(), f) }
}

#[derive(Copy, Clone, Eq, PartialEq)]
enum Cpu { Native, X64V1, X64V2, X64V3, X64V4 }

impl Cpu {
    fn name(self) -> &'static str {
        match self {
            Cpu::Native => "native",
            Cpu::X64V1 => "x86-64-v1",
            Cpu::X64V2 => "x86-64-v2",
            Cpu::X64V3 => "x86-64-v3",
            Cpu::X64V4 => "x86-64-v4",
        }
    }

    fn rustflag_name(self) -> &'static str {
        match self {
            Cpu::Native => "native",
            Cpu::X64V1 => "x86-64",
            Cpu::X64V2 => "x86-64-v2",
            Cpu::X64V3 => "x86-64-v3",
            Cpu::X64V4 => "x86-64-v4",
        }
    }

    fn current() -> Vec<Self> {
        cfg_select! {
            target_arch = "x86_64" => {
                use std::arch::is_x86_feature_detected as f;

                let v2 = f!("cmpxchg16b") && f!("popcnt") && f!("sse3") && f!("sse4.1") &&
                    f!("sse4.2") && f!("ssse3");

                let v3 = v2 && f!("avx") && f!("avx2") && f!("bmi1") && f!("bmi2") && f!("f16c") &&
                    f!("fma") && f!("lzcnt") && f!("movbe") && f!("xsave");

                let v4 = v3 && f!("avx512bw") && f!("avx512cd") && f!("avx512dq") &&
                    f!("avx512f") && f!("avx512vl");

                match () {
                    () if v4 => vec![Cpu::X64V1, Cpu::X64V2, Cpu::X64V3, Cpu::X64V4],
                    () if v3 => vec![Cpu::X64V1, Cpu::X64V2, Cpu::X64V3],
                    () if v2 => vec![Cpu::X64V1, Cpu::X64V2],
                    () => vec![Cpu::X64V1],
                }
            },
            _ => vec![Cpu::Native],
        }
    }
}

impl fmt::Display for Cpu {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result { fmt::Display::fmt(self.name(), f) }
}

#[derive(Copy, Clone)]
enum CargoTool { Build, Zigbuild }

impl CargoTool {
    fn args(self) -> &'static [&'static str] {
        match self {
            CargoTool::Build => &["build"],
            CargoTool::Zigbuild => &["zigbuild"],
        }
    }

    fn ensure_installed(self) -> Result<(), MissingDependency> {
        match self {
            CargoTool::Build => Ok(()),

            CargoTool::Zigbuild => {
                process::Command::new("cargo").args(["zigbuild", "--help"])
                    .quiet().run()
                    .err_missing_dep("cargo-zigbuild", [
                        "cargo install --locked --git https://github.com/rust-cross/cargo-zigbuild \
                        --rev 110abf59 cargo-zigbuild",
                        "https://github.com/rust-cross/cargo-zigbuild",
                    ])?;

                let python_zig_installed = process::Command::new("python-zig").arg("version")
                    .quiet().run()
                    .is_ok();

                if !python_zig_installed {
                    process::Command::new("zig").arg("version")
                        .quiet().run()
                        .err_missing_dep("the zig programming language", [
                            "pip3 install ziglang",
                            "https://ziglang.org/",
                        ])?;
                }

                Ok(())
            },
        }
    }
}

struct MissingDependency {
    name: String,
    help: Vec<String>,
    err: anyhow::Error,
}

impl MissingDependency {
    fn new(
        name: impl ToString,
        help: impl IntoIterator<Item: ToString>,
        err: impl Into<anyhow::Error>,
    ) -> Self {
        MissingDependency {
            name: name.to_string(),
            help: help.into_iter().map(|item| item.to_string()).collect(),
            err: err.into(),
        }
    }
}

trait ErrorMissingDependency {
    type Output;
    fn err_missing_dep(
        self, name: impl ToString, help: impl IntoIterator<Item: ToString>,
    ) -> Self::Output;
}

impl<T, E: Into<anyhow::Error>> ErrorMissingDependency for Result<T, E> {
    type Output = Result<T, MissingDependency>;
    fn err_missing_dep(
        self, name: impl ToString, help: impl IntoIterator<Item: ToString>,
    ) -> Result<T, MissingDependency> {
        self.map_err(|err| MissingDependency::new(name, help, err))
    }
}

trait HandleMissingDependency {
    fn handle_missing_dep(self);
}

impl HandleMissingDependency for MissingDependency {
    fn handle_missing_dep(self) {
        let MissingDependency { name, help, err } = self;
        printwarn!("error checking required build dependency '{name}' installed: {err}");
        for help in help {
            printhelp!("{help}");
        }
    }
}

impl HandleMissingDependency for Result<(), MissingDependency> {
    fn handle_missing_dep(self) {
        if let Err(e) = self { e.handle_missing_dep() }
    }
}

trait CommandExt {
    fn quiet(&mut self) -> &mut Self;
    fn run(&mut self) -> anyhow::Result<()>;
    fn eval(&mut self) -> anyhow::Result<String>;
    fn display(&self) -> impl fmt::Display;
}

impl CommandExt for process::Command {
    fn quiet(&mut self) -> &mut Self {
        self.stdin(process::Stdio::null())
            .stdout(process::Stdio::null())
            .stderr(process::Stdio::null())
    }

    fn run(&mut self) -> anyhow::Result<()> {
        let display = self.display().to_string();
        let status = self.status().err_path(&display)?;

        if status.success() { Ok(()) }
        else { bail!("subprocess failed with {status}: {display}") }
    }

    fn eval(&mut self) -> anyhow::Result<String> {
        let display = self.display().to_string();
        let output = self.output().err_path(&display)?;
        let status = output.status;

        if status.success() { Ok(String::from_utf8_lossy_owned(output.stdout)) }
        else { bail!("subprocess failed with {status}: {display}") }
    }

    fn display(&self) -> impl fmt::Display {
        fn escape_item(item: &OsStr, is_arg: bool) -> String {
            let text = item.to_string_lossy();
            let unquoted_safe = !text.is_empty() && text.chars().all(|c| match c {
                '0'..='9' | 'A'..='Z' | 'a'..='z' => true,
                '%' | '+' | ',' | '-' | '.' | '/' | ':' | '@' | '_' => true,
                '=' if is_arg => true,
                _ => false,
            });

            if unquoted_safe { text.into_owned() } else {
                let escaped = text.replace('\'', r"'\''");
                format!("'{escaped}'")
            }
        }

        display_with(|f| {
            for (var, val) in self.get_envs() {
                let Some(val) = val else { continue };
                let (var, val) = (escape_item(var, false), escape_item(val, false));
                write!(f, "{var}={val} ")?;
            }

            let cmd = escape_item(self.get_program(), false);
            write!(f, "{cmd}")?;

            for arg in self.get_args() {
                let arg = escape_item(arg, true);
                write!(f, " {arg}")?;
            }

            Ok(())
        })
    }
}

trait ErrorPath {
    type Output;
    fn err_path(self, path: &(impl AsRef<Path> + ?Sized)) -> Self::Output;
}

impl<T> ErrorPath for io::Result<T> {
    type Output = anyhow::Result<T>;
    fn err_path(self, path: &(impl AsRef<Path> + ?Sized)) -> anyhow::Result<T> {
        let path = path.as_ref();
        self.map_err(|err| anyhow!("{err}: {}", path.display()))
    }
}

fn display_with(f: impl Fn(&mut fmt::Formatter) -> fmt::Result) -> impl fmt::Display {
    struct DisplayWith<F>(F);
    impl<F> fmt::Display for DisplayWith<F> where F: Fn(&mut fmt::Formatter) -> fmt::Result {
        fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result { (self.0)(f) }
    }
    DisplayWith(f)
}

macro_rules! printinfo {
    ($($t:tt)*) => {
        ::std::eprintln!(
            "{} {}",
            ::colored::Colorize::bold(&*::std::format!(
                "{} info:",
                ::colored::Colorize::bright_purple("[BUILDER]"),
            )),
            ::colored::Colorize::dimmed(&*::std::format!($($t)*)),
        )
    };
}
use printinfo;

macro_rules! printhelp {
    ($($t:tt)*) => {
        ::std::eprintln!(
            "{} {}",
            ::colored::Colorize::bold(&*::std::format!(
                "{} {}:",
                ::colored::Colorize::bright_purple("[BUILDER]"),
                ::colored::Colorize::bright_green("help"),
            )),
            ::colored::Colorize::dimmed(&*::std::format!($($t)*)),
        )
    };
}
use printhelp;

macro_rules! printwarn {
    ($($t:tt)*) => {
        ::std::eprintln!(
            "{} {}",
            ::colored::Colorize::bold(&*::std::format!(
                "{} {}:",
                ::colored::Colorize::bright_purple("[BUILDER]"),
                ::colored::Colorize::yellow("warning"),
            )),
            ::std::format_args!($($t)*),
        )
    };
}
use printwarn;

macro_rules! printerr {
    ($($t:tt)*) => {
        ::std::eprintln!(
            "{} {}",
            ::colored::Colorize::bold(&*::std::format!(
                "{} {}:",
                ::colored::Colorize::bright_purple("[BUILDER]"),
                ::colored::Colorize::bright_red("error"),
            )),
            ::std::format_args!($($t)*),
        )
    };
}
use printerr;
