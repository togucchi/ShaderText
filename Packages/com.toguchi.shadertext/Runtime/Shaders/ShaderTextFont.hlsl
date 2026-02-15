#ifndef SHADER_TEXT_FONT_INCLUDED
#define SHADER_TEXT_FONT_INCLUDED

// 5x7 bitmap font - 40 characters
// Index: 0-9 = '0'-'9', 10-35 = 'A'-'Z', 36 = ' ', 37 = ':', 38 = '.', 39 = '-'
// Encoding: bit_index = row * 5 + col (row 0 = top, col 0 = left)
// 35 bits total: .x = bits 0-31, .y = bits 32-34
#define SHADER_TEXT_CHAR_COUNT 40
#define SHADER_TEXT_GLYPH_W 5
#define SHADER_TEXT_GLYPH_H 7

static const uint2 _FontBitmap[SHADER_TEXT_CHAR_COUNT] = {
    uint2(0xA33AE62E, 0x3), // [ 0] '0'
    uint2(0x884210C4, 0x3), // [ 1] '1'
    uint2(0xC226422E, 0x7), // [ 2] '2'
    uint2(0xA306422E, 0x3), // [ 3] '3'
    uint2(0x11F4A988, 0x2), // [ 4] '4'
    uint2(0xA3083C3F, 0x3), // [ 5] '5'
    uint2(0xA317844C, 0x3), // [ 6] '6'
    uint2(0x8422221F, 0x0), // [ 7] '7'
    uint2(0xA317462E, 0x3), // [ 8] '8'
    uint2(0x910F462E, 0x1), // [ 9] '9'
    uint2(0x631FC62E, 0x4), // [10] 'A'
    uint2(0xE317C62F, 0x3), // [11] 'B'
    uint2(0xA210862E, 0x3), // [12] 'C'
    uint2(0xE318C62F, 0x3), // [13] 'D'
    uint2(0xC217843F, 0x7), // [14] 'E'
    uint2(0x4217843F, 0x0), // [15] 'F'
    uint2(0xA31E862E, 0x3), // [16] 'G'
    uint2(0x631FC631, 0x4), // [17] 'H'
    uint2(0x8842108E, 0x3), // [18] 'I'
    uint2(0x9284211C, 0x1), // [19] 'J'
    uint2(0x52519531, 0x4), // [20] 'K'
    uint2(0xC2108421, 0x7), // [21] 'L'
    uint2(0x631AD771, 0x4), // [22] 'M'
    uint2(0x631CD671, 0x4), // [23] 'N'
    uint2(0xA318C62E, 0x3), // [24] 'O'
    uint2(0x4217C62F, 0x0), // [25] 'P'
    uint2(0x9358C62E, 0x5), // [26] 'Q'
    uint2(0x5257C62F, 0x4), // [27] 'R'
    uint2(0xA307062E, 0x3), // [28] 'S'
    uint2(0x0842109F, 0x1), // [29] 'T'
    uint2(0xA318C631, 0x3), // [30] 'U'
    uint2(0x14A8C631, 0x1), // [31] 'V'
    uint2(0x775AC631, 0x4), // [32] 'W'
    uint2(0x62A22A31, 0x4), // [33] 'X'
    uint2(0x08422A31, 0x1), // [34] 'Y'
    uint2(0xC222221F, 0x7), // [35] 'Z'
    uint2(0x00000000, 0x0), // [36] ' '
    uint2(0x08401080, 0x0), // [37] ':'
    uint2(0x8C000000, 0x1), // [38] '.'
    uint2(0x000F8000, 0x0)  // [39] '-'
};

float SampleGlyph(uint charIndex, float2 uv)
{
    if (charIndex >= SHADER_TEXT_CHAR_COUNT)
        return 0.0;

    uint col = clamp((uint)(uv.x * SHADER_TEXT_GLYPH_W), 0, SHADER_TEXT_GLYPH_W - 1);
    uint row = clamp((uint)((1.0 - uv.y) * SHADER_TEXT_GLYPH_H), 0, SHADER_TEXT_GLYPH_H - 1);
    uint bitIndex = row * SHADER_TEXT_GLYPH_W + col;

    uint2 glyph = _FontBitmap[charIndex];
    uint word = bitIndex < 32u ? glyph.x : glyph.y;
    uint shift = bitIndex < 32u ? bitIndex : (bitIndex - 32u);

    return (float)((word >> shift) & 1u);
}

#endif // SHADER_TEXT_FONT_INCLUDED
