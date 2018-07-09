using System;
using System.Drawing;
using System.IO;

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

                return checkFormat(image);
            }
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
        /// フォーマットをチェックする
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Format checkFormat(byte[] image)
        {
            // エラーチェック
            if (image.Length < 2)
            {
                return Format.Invalid;
            }

            // 先頭のマジックナンバーをチェックする
            // P1 ～ P6
            int magicNumber = 0;
            if ((char)image[0] == 'P' && int.TryParse(image[1].ToString(), out magicNumber))
            {
                if (magicNumber >= 1 && magicNumber <= 6)
                {
                    return (Format)Enum.ToObject(typeof(Format), magicNumber);
                }
            }
            return Format.Invalid;
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
        private static int[] _delimiters = { 0x20, 0x0d, 0x0a, 0x09 };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int skipDelimiters(byte [] image, int offset, out int count)
        {
            count = 0;
            for (int index = offset; index < image.Length; ++index)
            {
                for (int d = 0; d < _delimiters.Length; ++d)
                {
                    if (image[index] != _delimiters[d])
                    {
                        count = index - offset;
                        return index;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap convertPBM(byte [] image)
        {
            int offset = 2; // 先頭のマジックナンバーを飛ばす

            int count = 0;
            skipDelimiters(image, offset, out count);

            return null;
        }
    }
}
