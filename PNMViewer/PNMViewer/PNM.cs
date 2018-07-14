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

                if (checkPBMHeader(image)) return Format.PBM;
                return checkFormat(image);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Bitmap Convert(string filename)
        {
            return convert(filename);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Bitmap ToBitmap()
        {
            return null;
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
        /// 
        /// </summary>
        private static string _metaDelimiters = "[ \r\n\t]";

        /// <summary>
        /// 
        /// </summary>
        private static string _metaDelimitersOneOrMore = _metaDelimiters + "+";

        /// <summary>
        /// 
        /// </summary>
        private static string _metaComment = "#.*\n";

        /// <summary>
        /// フォーマットをチェックする
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Format checkFormat(byte[] image)
        {
            // ASCII 文字として解釈し、コメントを削除する 
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
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool checkPBMHeader(byte [] image)
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
                        if ((line - 1) > 70) return false;
                        line = 0;
                        continue;
                    }
                    raw.Add(int.Parse(b.ToString()));
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static bool checkPGMHeader(byte[] image)
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
                return true;
            }
            return false;
        }

        /// <summary>
        /// コメントを削除する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string cutPNMComment(string text)
        {
            return Regex.Replace(text, "#.*\n", string.Empty);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int skipDelimiters(byte [] image, int offset, out int count)
        {
            count = 0;
            /*for (int index = offset; index < image.Length; ++index)
            {
                for (int d = 0; d < _delimiters.Length; ++d)
                {
                    if (image[index] != _delimiters[d])
                    {
                        count = index - offset;
                        return index;
                    }
                }
            }*/
            return 0;
        }

        /// <summary>
        /// 
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
                        if ((line - 1) > 70) return null;
                        line = 0;
                        continue;
                    }
                    raw.Add(int.Parse(b.ToString()));
                }
                Bitmap bitmap = new Bitmap(width, height);

#if false
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        int index = x + (y * width);
                        bitmap.SetPixel(x, y, raw[index] == 0 ? Color.White : Color.Black);
                    }
                }
#else
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
#endif
                return bitmap;
            }
            return null;
        }
    }
}
