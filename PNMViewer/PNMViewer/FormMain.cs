using System;
using System.Drawing;
using System.Windows.Forms;

namespace PNMViewer
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FormMain()
        {
            InitializeComponent();

            // ドラッグ＆ドロップを許可
            pictureBoxMain.AllowDrop = true;

            // 画像を開くまで保存は無効にする
            toolStripMenuItemSave.Enabled = false;

            // マウスで移動できるようにする
            Point mousePoint = new Point();
            pictureBoxMain.MouseDown += (object sender, MouseEventArgs e) =>
            {
                if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
                {
                    mousePoint = new Point(e.X, e.Y);
                }
            };
            pictureBoxMain.MouseMove += (object sender, MouseEventArgs e) =>
            {
                if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
                {
                    this.Location = new Point(
                        this.Location.X + e.X - mousePoint.X,
                        this.Location.Y + e.Y - mousePoint.Y);
                }
            };
            // ドラッグ＆ドロップイベント
            pictureBoxMain.DragDrop += (object sender, DragEventArgs e) =>
            {
                var filename = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                reloadPictureBox(filename[0]);
            };
            pictureBoxMain.DragEnter += (object sender, DragEventArgs e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            string[] cmds = Environment.GetCommandLineArgs();
            for (int i = 1; i < cmds.Length;)
            {
                reloadPictureBox(cmds[i]);
                break;
            }
        }

        /// <summary>
        /// パラメータの初期化
        /// </summary>
        private void reloadPictureBox(string filename)
        {
            Bitmap bitmap = createBitmapFromFile(filename);
            if (bitmap == null)
            {
                string errorMessage = string.Empty;
                switch (PNM.GetLastError())
                {
                    case PNM.ConvertResult.InvalidFormat:
                        errorMessage = "無効なフォーマットです.";
                        break;
                    case PNM.ConvertResult.Over70CharsPerLine:
                        errorMessage = "1 行が 70 文字を超えています.";
                        break;
                    case PNM.ConvertResult.NotSupportedFormat:
                        errorMessage = "未対応のフォーマットです.";
                        break;
                    default:
                        return;
                }
                MessageBox.Show(errorMessage, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _bitmap = bitmap;
            pictureBoxMain.Image = bitmap;
            fitFormSize();
            toolStripMenuItemSave.Enabled = true;
        }

        /// <summary>
        /// 画像読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Bitmap createBitmapFromFile(string filename)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(
                     filename,
                     System.IO.FileMode.Open,
                     System.IO.FileAccess.Read))
            {
                try
                {
                    return new Bitmap(Image.FromStream(fs));
                }
                catch (Exception)
                {
                    return PNM.FromFile(filename);
                }
            }
        }


        /// <summary>
        /// Open メニューが選択されたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PNM ファイル(*.pbm;*.pgm;*.ppm)|*.pbm;*.pgm;*.ppm|すべてのファイル(*.*)|*.*";
            ofd.Title = "開くファイルを選択してください";
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                reloadPictureBox(ofd.FileName);
            }
        }

        /// <summary>
        /// Save メニューが選択されたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Portable Network Graphics (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp|Graphics Interchange Format (*.gif)|*.gif";
            sfd.Title = "ファイル名を入力してください";
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                pictureBoxMain.Image.Save(sfd.FileName);
            }
        }

        /// <summary>
        /// Exit メニューが選択されたときのイベント（共通）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Hide Menu メニューが選択された時のイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemHideMenu_Click(object sender, EventArgs e)
        {
            toolStripMenuItemHideMenu.Checked = !toolStripMenuItemHideMenu.Checked;
            toolStripContextMenuItemHideMenu.Checked = toolStripMenuItemHideMenu.Checked;

            menuStripMain.Visible = !toolStripMenuItemHideMenu.Checked;

            if (toolStripMenuItemHideMenu.Checked)
            {
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
            fitFormSize();
        }

        /// <summary>
        /// Hide Menu コンテキストメニューが選択された時のイベント 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripContextMenuItemHideMenu_Click(object sender, EventArgs e)
        {
            toolStripContextMenuItemHideMenu.Checked = !toolStripContextMenuItemHideMenu.Checked;
            toolStripMenuItemHideMenu.Checked = toolStripContextMenuItemHideMenu.Checked;

            menuStripMain.Visible = !toolStripContextMenuItemHideMenu.Checked;

            if (toolStripContextMenuItemHideMenu.Checked)
            {
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
            fitFormSize();
        }

        /// <summary>
        /// フォームサイズの更新
        /// </summary>
        private void fitFormSize()
        {
            if (_bitmap != null)
            {
                Size = _bitmap.Size;
            }
        }

        /// <summary>
        /// 読み込み中の画像
        /// </summary>
        private Bitmap _bitmap;
    }
}
