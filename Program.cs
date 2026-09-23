using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FileRecoveryApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private ListView listView;
        private Button btnLoad;
        private Button btnRestore;
        private List<RecycleItem> items = new List<RecycleItem>();

        public MainForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "مستعيد الملفات - سلة المحذوفات";
            Width = 900;
            Height = 600;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterScreen;

            listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.Columns.Add("الاسم", 300);
            listView.Columns.Add("المسار الأصلي", 500);

            btnLoad = new Button();
            btnLoad.Text = "تحميل محتويات سلة المحذوفات";
            btnLoad.Dock = DockStyle.Top;
            btnLoad.Height = 45;
            btnLoad.Click += (s, e) => LoadRecycleBin();

            btnRestore = new Button();
            btnRestore.Text = "استعادة الملف المحدد";
            btnRestore.Dock = DockStyle.Bottom;
            btnRestore.Height = 45;
            btnRestore.Click += (s, e) => RestoreSelected();

            Controls.Add(listView);
            Controls.Add(btnLoad);
            Controls.Add(btnRestore);
        }

        private void LoadRecycleBin()
        {
            listView.Items.Clear();
            items.Clear();

            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic recycleBin = shell.NameSpace(10);
                dynamic folderItems = recycleBin.Items();
                int count = folderItems.Count;

                for (int i = 0; i < count; i++)
                {
                    dynamic item = folderItems.Item(i);
                    string name = item.Name;
                    string path = item.Path;
                    string originalPath = "";

                    try
                    {
                        originalPath = item.ExtendedProperty("System.Recycle.DeletedFrom");
                    }
                    catch { }

                    var rec = new RecycleItem
                    {
                        Name = name,
                        Path = path,
                        OriginalPath = originalPath
                    };

                    items.Add(rec);

                    var lvi = new ListViewItem(new string[] { name, originalPath });
                    lvi.Tag = rec;
                    listView.Items.Add(lvi);
                }

                if (listView.Items.Count == 0)
                {
                    MessageBox.Show("سلة المحذوفات فارغة أو لا يمكن الوصول إليها.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ: " + ex.Message);
            }
        }

        private void RestoreSelected()
        {
            if (listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("حدد ملفًا أولًا.");
                return;
            }

            var rec = (RecycleItem)listView.SelectedItems[0].Tag;

            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic recycleBin = shell.NameSpace(10);
                dynamic folderItems = recycleBin.Items();
                int count = folderItems.Count;

                for (int i = 0; i < count; i++)
                {
                    dynamic item = folderItems.Item(i);
                    if (item.Path == rec.Path)
                    {
                        item.InvokeVerb();
                        break;
                    }
                }

                MessageBox.Show("تمت محاولة الاستعادة.");
                LoadRecycleBin();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل الاستعادة: " + ex.Message);
            }
        }
    }

    public class RecycleItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string OriginalPath { get; set; }
    }
}
