using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PNMViewer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();

            initParam();
        }

        /// <summary>
        /// パラメータの初期化
        /// </summary>
        private void initParam()
        {
            pictureBoxMain.AllowDrop = true;
        }

        /// <summary>
        /// ドロップされたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxMain_DragDrop(object sender, DragEventArgs e)
        {
            var filename = (string [])e.Data.GetData(DataFormats.FileDrop, false);

            using (System.IO.FileStream fs = new System.IO.FileStream(
                     filename[0],
                     System.IO.FileMode.Open,
                     System.IO.FileAccess.Read))
            {
                try
                {
                    _bitmap = new Bitmap(Image.FromStream(fs));
                }
                catch(Exception)
                {
                    return;
                }
            }

            pictureBoxMain.Image = _bitmap;
        }

        /// <summary>
        /// ドラッグされたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 読み込み中の画像
        /// </summary>
        private Bitmap _bitmap;
    }
}
