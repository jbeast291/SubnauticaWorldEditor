use std::{ffi, ptr};
use anyhow::{anyhow, bail};
use once_cell::sync::OnceCell as OnceLock;

#[cfg(not(target_arch = "x86_64"))]
compile_error!("dispatcher only supported for x64");

#[cfg(not(any(
    feature = "cpu-native", feature = "cpu-x86-64-v1", feature = "cpu-x86-64-v2",
    feature = "cpu-x86-64-v3", feature = "cpu-x86-64-v4",
)))]
compile_error!("specify at least one target cpu");

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init_liboptoctrees(init: *const ffi::c_void) -> *mut ffi::c_void {
    match get_cdylib_init_func() {
        Ok(func) => unsafe { func(init) },
        Err(err) => {
            eprintln!("[liboptoctrees] error: {err}");
            ptr::null_mut()
        },
    }
}

type InitFunction = unsafe extern "C" fn(*const ffi::c_void) -> *mut ffi::c_void;

fn get_cdylib_init_func() -> anyhow::Result<&'static InitFunction> {
    static FUNC: OnceLock<libloading::Symbol<InitFunction>> = OnceLock::new();
    FUNC.get_or_try_init(|| {
        static DYLIB: OnceLock<(&str, libloading::Library)> = OnceLock::new();
        let (name, dylib) = DYLIB.get_or_try_init(load_cdylib)?;

        let func = unsafe { dylib.get::<InitFunction>(c"init_liboptoctrees") };
        func.map_err(|err| anyhow!(
            "failed to load expected function 'init_liboptoctrees' from native library '{name}' \
            because: {err}",
        ))
    })
    .map(|s| &**s)
}

fn load_cdylib() -> anyhow::Result<(&'static str, libloading::Library)> {
    let Some(cpu) = Cpu::current() else { bail!("cpu does not support required instructions") };
    let name = cfg_select! {
        target_os = "linux" => match cpu {
            #[cfg(feature = "cpu-native")] Cpu::Native => "liboptoctrees-native.so",
            #[cfg(feature = "cpu-x86-64-v1")] Cpu::X64V1 => "liboptoctrees-x86-64-v1.so",
            #[cfg(feature = "cpu-x86-64-v2")] Cpu::X64V2 => "liboptoctrees-x86-64-v2.so",
            #[cfg(feature = "cpu-x86-64-v3")] Cpu::X64V3 => "liboptoctrees-x86-64-v3.so",
            #[cfg(feature = "cpu-x86-64-v4")] Cpu::X64V4 => "liboptoctrees-x86-64-v4.so",
        },
        target_os = "windows" => match cpu {
            #[cfg(feature = "cpu-native")] Cpu::Native => "optoctrees-native.dll",
            #[cfg(feature = "cpu-x86-64-v1")] Cpu::X64V1 => "optoctrees-x86-64-v1.dll",
            #[cfg(feature = "cpu-x86-64-v2")] Cpu::X64V2 => "optoctrees-x86-64-v2.dll",
            #[cfg(feature = "cpu-x86-64-v3")] Cpu::X64V3 => "optoctrees-x86-64-v3.dll",
            #[cfg(feature = "cpu-x86-64-v4")] Cpu::X64V4 => "optoctrees-x86-64-v4.dll",
        },
        target_os = "macos" => match cpu {
            #[cfg(feature = "cpu-native")] Cpu::Native => "liboptoctrees-native.dylib",
            #[cfg(feature = "cpu-x86-64-v1")] Cpu::X64V1 => "liboptoctrees-x86-64-v1.dylib",
            #[cfg(feature = "cpu-x86-64-v2")] Cpu::X64V2 => "liboptoctrees-x86-64-v2.dylib",
            #[cfg(feature = "cpu-x86-64-v3")] Cpu::X64V3 => "liboptoctrees-x86-64-v3.dylib",
            #[cfg(feature = "cpu-x86-64-v4")] Cpu::X64V4 => "liboptoctrees-x86-64-v4.dylib",
        },
        _ => compile_error!("dispatcher only supported for linux, windows, or macos"),
    };

    let path = process_path::get_dylib_path();
    let path = path.as_ref().and_then(|path| path.parent().map(|path| {
        if path.is_empty() { ".".as_ref() } else { path }
    }));

    if let Some(path) = path && let Ok(lib) = unsafe { libloading::Library::new(path.join(name)) } {
        return Ok((name, lib))
    }

    let lib = unsafe { libloading::Library::new(name) }
        .map_err(|err| anyhow!("failed to load native library '{name}' because: {err}"))?;

    Ok((name, lib))
}

#[derive(Copy, Clone)]
enum Cpu {
    #[cfg(feature = "cpu-native")] Native,
    #[cfg(feature = "cpu-x86-64-v1")] X64V1,
    #[cfg(feature = "cpu-x86-64-v2")] X64V2,
    #[cfg(feature = "cpu-x86-64-v3")] X64V3,
    #[cfg(feature = "cpu-x86-64-v4")] X64V4,
}

impl Cpu {
    fn current() -> Option<Self> {
        #[cfg(any(feature = "cpu-x86-64-v2", feature = "cpu-x86-64-v3", feature = "cpu-x86-64-v4"))]
        use std::arch::is_x86_feature_detected as f;

        #[cfg(any(feature = "cpu-x86-64-v2", feature = "cpu-x86-64-v3", feature = "cpu-x86-64-v4"))]
        let v2 = f!("cmpxchg16b") && f!("popcnt") && f!("sse3") && f!("sse4.1") && f!("sse4.2") &&
            f!("ssse3");

        #[cfg(any(feature = "cpu-x86-64-v3", feature = "cpu-x86-64-v4"))]
        let v3 = v2 && f!("avx") && f!("avx2") && f!("bmi1") && f!("bmi2") && f!("f16c") &&
            f!("fma") && f!("lzcnt") && f!("movbe") && f!("xsave");

        #[cfg(feature = "cpu-x86-64-v4")]
        let v4 = v3 && f!("avx512bw") && f!("avx512cd") && f!("avx512dq") && f!("avx512f") &&
            f!("avx512vl");

        match () {
            #[cfg(feature = "cpu-x86-64-v4")] () if v4 => Some(Cpu::X64V4),
            #[cfg(feature = "cpu-x86-64-v3")] () if v3 => Some(Cpu::X64V3),
            #[cfg(feature = "cpu-x86-64-v2")] () if v2 => Some(Cpu::X64V2),
            () => cfg_select! {
                feature = "cpu-x86-64-v1" => Some(Cpu::X64V1),
                feature = "cpu-native" => Some(Cpu::Native),
                _ => None,
            },
        }
    }
}
