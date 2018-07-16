using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PNMViewer
{
    class PNM
    {
        /// <summary>
        /// PNM フォーマット
        /// </summary>
        public enum Format : UInt32
        {
            /// <summary>
            /// 無効なフォーマット
            /// </summary>
            Invalid,

            /// <summary>
            /// portable bitmap format (PBM 形式) 
            /// </summary>
            PBM,

            /// <summary>
            /// portable graymap format (PGM 形式)
            /// </summary>
            PGM,

            /// <summary>
            /// portable pixmap format (PPM 形式)
            /// </summary>
            PPM,

            /// <summary>
            /// portable bitmap format (PBM バイナリ形式) 
            /// </summary>
            PBM_Binary,

            /// <summary>
            /// portable graymap format (PGM バイナリ形式)
            /// </summary>
            PGM_Binary,

            /// <summary>
            /// portable pixmap format (PPM バイナリ形式)
            /// </summary>
            PPM_Binary,
        };

        /// <summary>
        /// コンバート結果
        /// </summary>
        public enum ConvertResult : UInt32
        {
            /// <summary>
            /// 初期状態
            /// </summary>
            None,

            /// <summary>
            /// 成功
            /// </summary>
            Success,

            /// <summary>
            /// 無効なフォーマット
            /// </summary>
            InvalidFormat,

            /// <summary>
            /// 1行あたりの文字数が 70 を超えている
            /// </summary>
            Over70CharsPerLine,

            /// <summary>
            /// 対応していないフォーマット
            /// </summary>
            NotSupportedFormat,
        };

        /// <summary>
        /// フォーマットをチェックする
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Format CheckFormat(string filename)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] image = new byte[fs.Length];
                fs.Read(image, 0, image.Length);
                return checkFormat(image);
            }
        }

        /// <summary>
        /// ファイルから画像を取得
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Bitmap FromFile(string filename)
        {
            return convert(filename);
        }

        /// <summary>
        /// 最後に発生したエラーを取得する
        /// </summary>
        /// <returns></returns>
        public static ConvertResult GetLastError()
        {
            return _lastError;
        }

        /// <summary>
        /// コンバート
        /// </summary>
        /// <param name="filename"></param>
        private static Bitmap convert(string filename)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] image = new byte[fs.Length];
                fs.Read(image, 0, image.Length);

                Format format = checkFormat(image);
                switch(format)
                {
                    case Format.PBM: return convertPBM(image);
                    case Format.PGM: return convertPGM(image);
                    case Format.PPM: return convertPPM(image);
                    case Format.PBM_Binary: return convertPBMRaw(image);
                    case Format.PGM_Binary: return convertPGMRaw(image);
                    case Format.PPM_Binary: return convertPPMRaw(image);
                }
            }
            return null;
        }

        /// <summary>
        /// 区切り文字コード
        /// スペース文字(' ')，CR('\r')，LF('\n')，TAB('\t')
        /// </summary>
        private static int[] _delimitersAscii = { 0x20, 0x0d, 0x0a, 0x09 };

        /// <summary>
        /// 区切り文字コード
        /// スペース文字(' ')，CR('\r')，LF('\n')，TAB('\t')
        /// </summary>
        private static char[] _delimitersChar = { ' ', '\r', '\n', '\t' };

        /// <summary>
        /// 一つの区切り文字を表すメタ文字
        /// </summary>
        private static string _metaDelimiters = "[ \r\n\t]";

        /// <summary>
        /// 一つ以上の区切り文字を表すメタ文字
        /// </summary>
        private static string _metaDelimitersOneOrMore = _metaDelimiters + "+";

        /// <summary>
        /// コメントを表すメタ文字
        /// </summary>
        private static string _metaComment = "#.*\n";

        /// <summary>
        /// ゼロ以上のコメントを表すメタ文字
        /// </summary>
        private static string _metaCommentZeroOrMore = "(" + _metaComment + ")*";

        /// <summary>
        /// フォーマットをチェックする
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Format checkFormat(byte[] image)
        {
            // ASCII 文字として解釈する 
            string all = Encoding.ASCII.GetString(image);

            // エラーチェック
            if (all.Length < 2)
            {
                return Format.Invalid;
            }

            // 先頭のマジックナンバーをチェックする
            // P1 ～ P6
            int magicNumber = 0;
            if ((char)all[0] == 'P' && int.TryParse(all[1].ToString(), out magicNumber))
            {
                if (magicNumber >= 1 && magicNumber <= 6)
                {
                    return (Format)Enum.ToObject(typeof(Format), magicNumber);
                }
            }
            return Format.Invalid;
        }

        /// <summary>
        /// コメントを削除する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string cutPNMComment(string text)
        {
            return Regex.Replace(text, _metaComment, string.Empty);
        }

        /// <summary>
        /// PBM 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPBM(byte [] image)
        {
            // ASCII 文字として解釈し、コメントを削除する 
            string all = Encoding.ASCII.GetString(image);
            all = Regex.Replace(all, _metaComment, string.Empty);

            // マジックナンバ「P1」
            // 単数または複数の「区切りコード」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数の「区切りコード」

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P1");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);

                int line = 0;
                var raw = new System.Collections.Generic.List<int>(width * height);
                for (int index = match.Length; index < all.Length; ++index, ++line)
                {
                    char b = all[index];
                    if (b == ' ' || b == '\r' || b == '\t') continue;
                    if (b == '\n')
                    {
                        // 1 行が 70 文字を超えている
                        if ((line - 1) > 70)
                        {
                            setLastError(ConvertResult.Over70CharsPerLine);
                            return null;
                        }
                        line = 0;
                        continue;
                    }
                    raw.Add(int.Parse(b.ToString()));
                }
                Bitmap bitmap = new Bitmap(width, height);

                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    byte color = raw[i] == 0 ? (byte)255 : (byte)0;
                    int index = i * 4;
                    buffer[index + 0] = color;
                    buffer[index + 1] = color;
                    buffer[index + 2] = color;
                    buffer[index + 3] = 255;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// PGM 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPGM(byte[] image)
        {
            // ASCII 文字として解釈し、コメントを削除する 
            string all = Encoding.ASCII.GetString(image);
            all = Regex.Replace(all, _metaComment, string.Empty);

            // マジックナンバ「P2」
            // 単数または複数の「区切りコード * 1」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数または複数の「区切りコード」
            // 画像の輝度の最大値
            // 単数の「区切りコード」

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P2");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<luminance>\\d+)");
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);
                int luminance = int.Parse(match.Groups["luminance"].Value);

                int line = 0;
                StringBuilder value = new StringBuilder();
                var raw = new System.Collections.Generic.List<int>(width * height);
                for (int index = match.Length; index < all.Length; ++index, ++line)
                {
                    char b = all[index];
                    bool checkValue = (b == ' ' || b == '\r' || b == '\t');

                    if (b == '\n')
                    {
                        // 1 行が 70 文字を超えている
                        if ((line - 1) > 70)
                        {
                            setLastError(ConvertResult.Over70CharsPerLine);
                            return null;
                        }
                        line = 0;
                        checkValue = true;
                    }

                    if (checkValue)
                    {
                        if (value.Length > 0)
                        {
                            raw.Add(int.Parse(value.ToString()));
                            value.Clear();
                        }
                        continue;
                    }

                    value.Append(b);
                }
                Bitmap bitmap = new Bitmap(width, height);

                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    byte color = (byte)(255 * (raw[i] / (float)luminance));
                    int index = i * 4;
                    buffer[index + 0] = color;
                    buffer[index + 1] = color;
                    buffer[index + 2] = color;
                    buffer[index + 3] = 255;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// PPM 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPPM(byte[] image)
        {
            // ASCII 文字として解釈し、コメントを削除する 
            string all = Encoding.ASCII.GetString(image);
            all = Regex.Replace(all, _metaComment, string.Empty);

            // マジックナンバ「P3」
            // 単数または複数の「区切りコード * 1」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数または複数の「区切りコード」
            // RGBカラー画像値の最大値
            // 単数の「区切りコード」

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P3");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append("(?<max>\\d+)");
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);
                int max = int.Parse(match.Groups["max"].Value);

                int line = 0;
                StringBuilder value = new StringBuilder();
                var colorList = new System.Collections.Generic.List<int>();
                var raw = new System.Collections.Generic.List<Color>(width * height);
                for (int index = match.Length; index < all.Length; ++index, ++line)
                {
                    char b = all[index];
                    bool checkValue = (b == ' ' || b == '\r' || b == '\t');

                    if (b == '\n')
                    {
                        // 1 行が 70 文字を超えている
                        if ((line - 1) > 70)
                        {
                            setLastError(ConvertResult.Over70CharsPerLine);
                            return null;
                        }
                        line = 0;
                        checkValue = true;
                    }

                    if (checkValue)
                    {
                        if (value.Length > 0)
                        {
                            if (colorList.Count >= 3)
                            {
                                raw.Add(Color.FromArgb(255,
                                    (byte)(255 * (colorList[0] / (float)max)),
                                    (byte)(255 * (colorList[1] / (float)max)),
                                    (byte)(255 * (colorList[2] / (float)max))
                                ));
                                colorList.Clear();
                            }
                            colorList.Add(int.Parse(value.ToString()));
                            value.Clear();
                        }
                        continue;
                    }

                    value.Append(b);
                }
                Bitmap bitmap = new Bitmap(width, height);

                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    int index = i * 4;
                    buffer[index + 0] = raw[i].B;
                    buffer[index + 1] = raw[i].G;
                    buffer[index + 2] = raw[i].R;
                    buffer[index + 3] = raw[i].A;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// PBM Raw 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPBMRaw(byte[] image)
        {
            // ASCII 文字として解釈する
            string all = Encoding.ASCII.GetString(image);

            // マジックナンバ「P4」
            // 単数または複数の「区切りコード」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数の「区切りコード」

            // コメントは事前に削除するべきだけど
            // バイナリ部分にコメントと判断できる部分がある場合があるので、正規表現の判定に含める

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P4");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);

                int count = 0;
                var raw = new System.Collections.Generic.List<int>(width * height);

                for (int index = match.Length; index < image.Length; ++index)
                {
                    byte b = image[index];
                    for (int x = 0; x < 8; ++x)
                    {
                        int c = (b >> (8 - (x + 1))) & 0x01;
                        raw.Add(c);

                        if (++count >= width)
                        {
                            count = 0;
                            break;
                        }
                    }
                }

                Bitmap bitmap = new Bitmap(width, height);
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    byte color = raw[i] == 0 ? (byte)255 : (byte)0;
                    int index = i * 4;
                    buffer[index + 0] = color;
                    buffer[index + 1] = color;
                    buffer[index + 2] = color;
                    buffer[index + 3] = 255;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// PGM Raw 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPGMRaw(byte[] image)
        {
            // ASCII 文字として解釈する
            string all = Encoding.ASCII.GetString(image);

            // マジックナンバ「P5」
            // 単数または複数の「区切りコード * 1」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数または複数の「区切りコード」
            // 画像の輝度の最大値
            // 単数の「区切りコード」

            // コメントは事前に削除するべきだけど
            // バイナリ部分にコメントと判断できる部分がある場合があるので、正規表現の判定に含める

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P5");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<luminance>\\d+)");
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);
                int luminance = int.Parse(match.Groups["luminance"].Value);

                if (luminance > 255)
                {
                    // 最大輝度が 255 以上の場合は 65535 以下の数値が入り
                    // 1 ピクセル 2 バイトとする必要がある
                    // そのうち対応する
                    setLastError(ConvertResult.NotSupportedFormat);
                    return null;
                }

                var raw = new System.Collections.Generic.List<int>(width * height);
                for (int index = match.Length; index < image.Length; ++index)
                {
                    byte b = image[index];
                    raw.Add((int)b);
                }

                Bitmap bitmap = new Bitmap(width, height);
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    byte color = (byte)(255 * (raw[i] / (float)luminance));
                    int index = i * 4;
                    buffer[index + 0] = color;
                    buffer[index + 1] = color;
                    buffer[index + 2] = color;
                    buffer[index + 3] = 255;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// PPM Raw 画像作成
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPPMRaw(byte[] image)
        {
            // ASCII 文字として解釈する
            string all = Encoding.ASCII.GetString(image);

            // マジックナンバ「P6」
            // 単数または複数の「区切りコード * 1」
            // 画像の横方向ピクセル数（画像の横幅）
            // 単数または複数の「区切りコード」
            // 画像の縦方向ピクセル数（画像の高さ）
            // 単数または複数の「区切りコード」
            // RGBカラー画像値の最大値
            // 単数の「区切りコード」

            // コメントは事前に削除するべきだけど
            // バイナリ部分にコメントと判断できる部分がある場合があるので、正規表現の判定に含める

            StringBuilder pattern = new StringBuilder();
            pattern.Append("P6");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<width>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<height>\\d+)");
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append(_metaDelimitersOneOrMore);
            pattern.Append(_metaCommentZeroOrMore);
            pattern.Append("(?<max>\\d+)");
            pattern.Append(_metaDelimiters);

            Regex r = new Regex(pattern.ToString());
            Match match = r.Match(all);

            if (match.Success)
            {
                int width = int.Parse(match.Groups["width"].Value);
                int height = int.Parse(match.Groups["height"].Value);
                int max = int.Parse(match.Groups["max"].Value);

                if (max > 255)
                {
                    // 最大 RGB 値が 255 以上の場合は 65535 以下の数値が入り
                    // 1 ピクセル 2 バイトとする必要がある
                    // そのうち対応する
                    setLastError(ConvertResult.NotSupportedFormat);
                    return null;
                }

                int line = 0;
                StringBuilder value = new StringBuilder();
                var colorList = new System.Collections.Generic.List<int>();
                var raw = new System.Collections.Generic.List<Color>(width * height);
                for (int index = match.Length; index < all.Length; ++index, ++line)
                {
                    byte b = image[index];

                    if (value.Length > 0)
                    {
                        if (colorList.Count >= 3)
                        {
                            raw.Add(Color.FromArgb(255,
                                (byte)(255 * (colorList[0] / (float)max)),
                                (byte)(255 * (colorList[1] / (float)max)),
                                (byte)(255 * (colorList[2] / (float)max))
                            ));
                            colorList.Clear();
                        }
                        colorList.Add(int.Parse(value.ToString()));
                        value.Clear();
                    }
                    value.Append(b.ToString());
                }
                Bitmap bitmap = new Bitmap(width, height);

                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                for (int i = 0; i < raw.Count; ++i)
                {
                    int index = i * 4;
                    buffer[index + 0] = raw[i].B;
                    buffer[index + 1] = raw[i].G;
                    buffer[index + 2] = raw[i].R;
                    buffer[index + 3] = raw[i].A;
                }
                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                setLastError(ConvertResult.Success);
                return bitmap;
            }

            setLastError(ConvertResult.InvalidFormat);
            return null;
        }

        /// <summary>
        /// 最後に発生したエラーを設定
        /// </summary>
        /// <param name="error"></param>
        private static void setLastError(ConvertResult error)
        {
            _lastError = error;
        }

        /// <summary>
        /// 最後に発生したエラー
        /// </summary>
        private static ConvertResult _lastError = ConvertResult.None;
    }
}
