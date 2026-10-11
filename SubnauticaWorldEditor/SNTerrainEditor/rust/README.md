# `liboptoctrees`

To-do list for Jbeast:

- Make sure the new API works in-game.
  - Compile with `cargo build --release` to test this first.
- Make sure the Windows GNU LLVM target works for Subnautica modding.
  - Run `rustup target add x86_64-pc-windows-gnullvm` first.
  - Install [LLVM MinGW] to allow compiling with this target.
    - Read the compiler's documentation on [`*-windows-gnullvm` targets] to learn more.
  - Compile with `cargo build --release --target x86_64-pc-windows-gnullvm` and test in-game.
- Write a Windows version of `builder.sh` as `builder.bat`.
- Make sure runtime CPU feature dispatch works in-game.
  - Run `builder.bat build` and take the output from `target/builder`.

[LLVM MinGW]: https://github.com/mstorsjo/llvm-mingw
[`*-windows-gnullvm` targets]: https://doc.rust-lang.org/rustc/platform-support/windows-gnullvm.html
