use wide::{u8x16, u8x32, u8x64};
use crate::{u8xN, NATIVE_SIMD_WIDTH};

#[cfg(all(target_arch = "x86_64", target_feature = "avx2"))]
use std::arch::x86_64;

#[expect(nonstandard_style)]
#[inline(always)]
pub fn transpose_u8_Nx16(matrix: [u8x16; NATIVE_SIMD_WIDTH]) -> [u8xN; 16] {
    cfg_select! {
        cpu = "x86-64-v4" => transpose_u8_64x16(matrix),
        cpu = "x86-64-v3" => transpose_u8_32x16(matrix),
        _ => transpose_u8_16x16(matrix),
    }
}

#[cfg_attr(all(not(test), not(cpu = "x86-64-v4")), expect(unused))]
#[inline(always)]
fn transpose_u8_64x16(matrix: [u8x16; 64]) -> [u8x64; 16] {
    let matrix = [
        [matrix[0], matrix[16], matrix[32], matrix[48]],
        [matrix[1], matrix[17], matrix[33], matrix[49]],
        [matrix[2], matrix[18], matrix[34], matrix[50]],
        [matrix[3], matrix[19], matrix[35], matrix[51]],
        [matrix[4], matrix[20], matrix[36], matrix[52]],
        [matrix[5], matrix[21], matrix[37], matrix[53]],
        [matrix[6], matrix[22], matrix[38], matrix[54]],
        [matrix[7], matrix[23], matrix[39], matrix[55]],
        [matrix[8], matrix[24], matrix[40], matrix[56]],
        [matrix[9], matrix[25], matrix[41], matrix[57]],
        [matrix[10], matrix[26], matrix[42], matrix[58]],
        [matrix[11], matrix[27], matrix[43], matrix[59]],
        [matrix[12], matrix[28], matrix[44], matrix[60]],
        [matrix[13], matrix[29], matrix[45], matrix[61]],
        [matrix[14], matrix[30], matrix[46], matrix[62]],
        [matrix[15], matrix[31], matrix[47], matrix[63]],
    ];

    transpose_u8_16x16_impl::<u8x64>(bytemuck::must_cast(matrix))
}

#[cfg_attr(all(not(test), any(cpu = "x86-64-v4", not(cpu = "x86-64-v3"))), expect(unused))]
#[inline(always)]
fn transpose_u8_32x16(matrix: [u8x16; 32]) -> [u8x32; 16] {
    let matrix = [
        [matrix[0], matrix[16]], [matrix[1], matrix[17]],
        [matrix[2], matrix[18]], [matrix[3], matrix[19]],
        [matrix[4], matrix[20]], [matrix[5], matrix[21]],
        [matrix[6], matrix[22]], [matrix[7], matrix[23]],
        [matrix[8], matrix[24]], [matrix[9], matrix[25]],
        [matrix[10], matrix[26]], [matrix[11], matrix[27]],
        [matrix[12], matrix[28]], [matrix[13], matrix[29]],
        [matrix[14], matrix[30]], [matrix[15], matrix[31]],
    ];

    transpose_u8_16x16_impl::<u8x32>(bytemuck::must_cast(matrix))
}

#[cfg_attr(all(not(test), cpu = "x86-64-v3"), expect(unused))]
#[inline(always)]
fn transpose_u8_16x16(matrix: [u8x16; 16]) -> [u8x16; 16] {
    transpose_u8_16x16_impl(matrix)
}

#[inline(always)]
fn transpose_u8_16x16_impl<T: SimdUnpackHalf + Copy>(matrix: [T; 16]) -> [T; 16] {
    #[inline(always)]
    fn step<T: SimdUnpackHalf + Copy>(matrix: [T; 16]) -> [T; 16] {
        [
            T::unpack_low(matrix[0], matrix[8]), T::unpack_high(matrix[0], matrix[8]),
            T::unpack_low(matrix[1], matrix[9]), T::unpack_high(matrix[1], matrix[9]),
            T::unpack_low(matrix[2], matrix[10]), T::unpack_high(matrix[2], matrix[10]),
            T::unpack_low(matrix[3], matrix[11]), T::unpack_high(matrix[3], matrix[11]),
            T::unpack_low(matrix[4], matrix[12]), T::unpack_high(matrix[4], matrix[12]),
            T::unpack_low(matrix[5], matrix[13]), T::unpack_high(matrix[5], matrix[13]),
            T::unpack_low(matrix[6], matrix[14]), T::unpack_high(matrix[6], matrix[14]),
            T::unpack_low(matrix[7], matrix[15]), T::unpack_high(matrix[7], matrix[15]),
        ]
    }

    step(step(step(step(matrix))))
}

#[expect(nonstandard_style)]
#[inline(always)]
pub fn transpose_u8_Nx8(matrix: [u8xN; 8]) -> [u8xN; 8] {
    cfg_select! {
        cpu = "x86-64-v4" => transpose_u8_64x8(matrix),
        cpu = "x86-64-v3" => transpose_u8_32x8(matrix),
        _ => transpose_u8_16x8(matrix),
    }
}

#[cfg_attr(all(not(test), not(cpu = "x86-64-v4")), expect(unused))]
#[inline(always)]
fn transpose_u8_64x8(matrix: [u8x64; 8]) -> [u8x64; 8] {
    let matrix: [u8x16; 32] = bytemuck::must_cast(matrix);
    let matrix = [
        [matrix[0], matrix[8], matrix[16], matrix[24]],
        [matrix[1], matrix[9], matrix[17], matrix[25]],
        [matrix[2], matrix[10], matrix[18], matrix[26]],
        [matrix[3], matrix[11], matrix[19], matrix[27]],
        [matrix[4], matrix[12], matrix[20], matrix[28]],
        [matrix[5], matrix[13], matrix[21], matrix[29]],
        [matrix[6], matrix[14], matrix[22], matrix[30]],
        [matrix[7], matrix[15], matrix[23], matrix[31]],
    ];

    transpose_u8_16x8_impl::<u8x64>(bytemuck::must_cast(matrix))
}

#[cfg_attr(all(not(test), any(cpu = "x86-64-v4", not(cpu = "x86-64-v3"))), expect(unused))]
#[inline(always)]
fn transpose_u8_32x8(matrix: [u8x32; 8]) -> [u8x32; 8] {
    let matrix: [u8x16; 16] = bytemuck::must_cast(matrix);
    let matrix = [
        [matrix[0], matrix[8]], [matrix[1], matrix[9]],
        [matrix[2], matrix[10]], [matrix[3], matrix[11]],
        [matrix[4], matrix[12]], [matrix[5], matrix[13]],
        [matrix[6], matrix[14]], [matrix[7], matrix[15]],
    ];

    transpose_u8_16x8_impl::<u8x32>(bytemuck::must_cast(matrix))
}

#[cfg_attr(all(not(test), cpu = "x86-64-v3"), expect(unused))]
#[inline(always)]
fn transpose_u8_16x8(matrix: [u8x16; 8]) -> [u8x16; 8] {
    transpose_u8_16x8_impl(matrix)
}

#[inline(always)]
fn transpose_u8_16x8_impl<T: SimdUnpackHalf + Copy>(matrix: [T; 8]) -> [T; 8] {
    #[inline(always)]
    fn step<T: SimdUnpackHalf + Copy>(matrix: [T; 8]) -> [T; 8] {
        [
            T::unpack_low(matrix[0], matrix[4]), T::unpack_high(matrix[0], matrix[4]),
            T::unpack_low(matrix[1], matrix[5]), T::unpack_high(matrix[1], matrix[5]),
            T::unpack_low(matrix[2], matrix[6]), T::unpack_high(matrix[2], matrix[6]),
            T::unpack_low(matrix[3], matrix[7]), T::unpack_high(matrix[3], matrix[7]),
        ]
    }

    step(step(step(step(matrix))))
}

trait SimdUnpackHalf {
    fn unpack_low(lhs: Self, rhs: Self) -> Self;
    fn unpack_high(lhs: Self, rhs: Self) -> Self;
}

impl SimdUnpackHalf for u8x16 {
    #[inline(always)]
    fn unpack_low(lhs: Self, rhs: Self) -> Self { u8x16::unpack_low(lhs, rhs) }

    #[inline(always)]
    fn unpack_high(lhs: Self, rhs: Self) -> Self { u8x16::unpack_high(lhs, rhs) }
}

impl SimdUnpackHalf for u8x32 {
    #[inline(always)]
    fn unpack_low(lhs: Self, rhs: Self) -> Self {
        cfg_select! {
            all(target_arch = "x86_64", target_feature = "avx2") => {
                let [lhs, rhs] = bytemuck::must_cast([lhs, rhs]);

                // SAFETY: `avx2` is enabled per `cfg`.
                bytemuck::must_cast(unsafe { x86_64::_mm256_unpacklo_epi8(lhs, rhs) })
            },

            _ => {
                let [[la, lb], [ra, rb]] = bytemuck::must_cast([lhs, rhs]);
                bytemuck::must_cast([u8x16::unpack_low(la, ra), u8x16::unpack_low(lb, rb)])
            },
        }
    }

    #[inline(always)]
    fn unpack_high(lhs: Self, rhs: Self) -> Self {
        cfg_select! {
            all(target_arch = "x86_64", target_feature = "avx2") => {
                let [lhs, rhs] = bytemuck::must_cast([lhs, rhs]);

                // SAFETY: `avx2` is enabled per `cfg`.
                bytemuck::must_cast(unsafe { x86_64::_mm256_unpackhi_epi8(lhs, rhs) })
            },

            _ => {
                let [[la, lb], [ra, rb]] = bytemuck::must_cast([lhs, rhs]);
                bytemuck::must_cast([u8x16::unpack_high(la, ra), u8x16::unpack_high(lb, rb)])
            },
        }
    }
}

impl SimdUnpackHalf for u8x64 {
    #[inline(always)]
    fn unpack_low(lhs: Self, rhs: Self) -> Self {
        cfg_select! {
            all(target_arch = "x86_64", target_feature = "avx512bw") => {
                let [lhs, rhs] = bytemuck::must_cast([lhs, rhs]);

                // SAFETY: `avx512bw` is enabled per `cfg`.
                bytemuck::must_cast(unsafe { x86_64::_mm512_unpacklo_epi8(lhs, rhs) })
            },

            _ => {
                let [[la, lb], [ra, rb]] = bytemuck::must_cast([lhs, rhs]);
                bytemuck::must_cast([u8x32::unpack_low(la, ra), u8x32::unpack_low(lb, rb)])
            },
        }
    }

    #[inline(always)]
    fn unpack_high(lhs: Self, rhs: Self) -> Self {
        cfg_select! {
            all(target_arch = "x86_64", target_feature = "avx512bw") => {
                let [lhs, rhs] = bytemuck::must_cast([lhs, rhs]);

                // SAFETY: `avx512bw` is enabled per `cfg`.
                bytemuck::must_cast(unsafe { x86_64::_mm512_unpackhi_epi8(lhs, rhs) })
            },

            _ => {
                let [[la, lb], [ra, rb]] = bytemuck::must_cast([lhs, rhs]);
                bytemuck::must_cast([u8x32::unpack_high(la, ra), u8x32::unpack_high(lb, rb)])
            },
        }
    }
}

#[cfg(test)]
mod test {
    use super::*;
    use std::array;

    #[test]
    fn transpose() {
        fn input<const R: usize, const C: usize, T: From<[u8; C]>>(width: u8) -> [T; R] {
            array::from_fn(|r| T::from(array::from_fn(|c| {
                (r as u8).wrapping_mul(width) + c as u8
            })))
        }

        fn expected<const R: usize, const C: usize, T: From<[u8; C]>>(width: u8) -> [T; R] {
            array::from_fn(|r| T::from(array::from_fn(|c| {
                (c as u8).wrapping_mul(width) + r as u8
            })))
        }

        assert_eq!(transpose_u8_64x16(input(16)), expected(16), "transpose_u8_64x16");
        assert_eq!(transpose_u8_32x16(input(16)), expected(16), "transpose_u8_32x16");
        assert_eq!(transpose_u8_16x16(input(16)), expected(16), "transpose_u8_16x16");
        assert_eq!(transpose_u8_64x8(input(64)), expected(8), "transpose_u8_64x8");
        assert_eq!(transpose_u8_32x8(input(32)), expected(8), "transpose_u8_32x8");
        assert_eq!(transpose_u8_16x8(input(16)), expected(8), "transpose_u8_16x8");
    }
}
