use build_rs::{input as get, output as emit};

fn main() {
    emit_cpu();
}

fn emit_cpu() {
    emit::rustc_check_cfg_values("cpu", &["x86-64-v1", "x86-64-v2", "x86-64-v3", "x86-64-v4"]);

    match &*get::cargo_cfg_target_arch() {
        "x86_64" => {
            emit::rustc_cfg_value("cpu", "x86-64-v1");
            let f = TargetFeatures::load();

            if f.all(&["cmpxchg16b", "popcnt", "sse3", "sse4.1", "sse4.2", "ssse3"]) {
                emit::rustc_cfg_value("cpu", "x86-64-v2");
            } else { return }

            if f.all(&["avx", "avx2", "bmi1", "bmi2", "f16c", "fma", "lzcnt", "movbe", "xsave"]) {
                emit::rustc_cfg_value("cpu", "x86-64-v3");
            } else { return }

            if f.all(&["avx512bw", "avx512cd", "avx512dq", "avx512f", "avx512vl"]) {
                emit::rustc_cfg_value("cpu", "x86-64-v4");
            } else { return }
        },

        _ => (),
    }
}

struct TargetFeatures {
    features: Vec<String>,
}

impl TargetFeatures {
    fn load() -> Self {
        let mut features = get::cargo_cfg_target_feature();
        features.sort_unstable();
        TargetFeatures { features }
    }

    fn has(&self, feature: &str) -> bool {
        self.features.binary_search_by_key(&feature, |f| f).is_ok()
    }

    fn all(&self, features: &[&str]) -> bool {
        features.iter().all(|f| self.has(f))
    }
}
