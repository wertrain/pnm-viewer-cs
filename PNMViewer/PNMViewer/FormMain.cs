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
        /// <summary>
        /// コンストラクタ
        /// </summary>
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

                using (System.IO.FileStream fs = new System.IO.FileStream(
                         filename[0],
                         System.IO.FileMode.Open,
                         System.IO.FileAccess.Read))
                {
                    try
                    {
                        _bitmap = new Bitmap(Image.FromStream(fs));
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }

                pictureBoxMain.Image = _bitmap;
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
        }

        /// <summary>
        /// 読み込み中の画像
        /// </summary>
        private Bitmap _bitmap;
    }
}
