using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ProxyGuy.WinForms
{
    public class PhpIniPathsForm : Form
    {
        private ListBox listPaths;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnOk;
        private Button btnCancel;

        public List<string> PhpIniPaths { get; } = new();

        public PhpIniPathsForm(IEnumerable<string> initialPaths)
        {
            Text = "Rutas php.ini";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 300);

            listPaths = new ListBox { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                                      Location = new Point(10, 10), Size = new Size(360, 200) };
            btnAdd = new Button { Text = "Añadir...", Location = new Point(10, 220), Size = new Size(80, 23) };
            btnRemove = new Button { Text = "Eliminar", Location = new Point(100, 220), Size = new Size(80, 23) };
            btnOk = new Button { Text = "OK", Location = new Point(210, 250), Size = new Size(75, 23), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancelar", Location = new Point(295, 250), Size = new Size(75, 23), DialogResult = DialogResult.Cancel };

            Controls.Add(listPaths);
            Controls.Add(btnAdd);
            Controls.Add(btnRemove);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnAdd.Click += BtnAdd_Click;
            btnRemove.Click += BtnRemove_Click;

            foreach (var p in initialPaths)
            {
                listPaths.Items.Add(p);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "INI files (*.ini)|*.ini|All files|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in ofd.FileNames)
                {
                    if (!listPaths.Items.Contains(file))
                        listPaths.Items.Add(file);
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            while (listPaths.SelectedItem != null)
            {
                listPaths.Items.Remove(listPaths.SelectedItem);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                PhpIniPaths.Clear();
                foreach (var item in listPaths.Items)
                    PhpIniPaths.Add(item.ToString());
            }
            base.OnFormClosing(e);
        }
    }
}
