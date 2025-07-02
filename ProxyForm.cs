using System;
using System.Windows.Forms;

namespace ProxyGuy.WinForms
{
    public partial class ProxyForm : Form
    {
        private ProxyService _proxy;
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<RequestInfo>> _domainRequests = new();
        private System.Collections.Generic.List<RequestInfo> _displayRequests = new();
        private string _domainFilter = string.Empty;
        private string _methodFilter = "All";
        private string _statusFilter = "All";
        private bool _proxyEnabled = false;
        private bool _phpIniModified = false;
        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            _domainRequests.Clear();
            _displayRequests.Clear();
            listDomains.Items.Clear();
            gridRequests.Rows.Clear();
            gridRequestHeaders.Rows.Clear();
            gridResponseHeaders.Rows.Clear();
            txtRequestBody.Clear();
            txtResponseBody.Clear();
        }

        private void gridRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (gridRequests.SelectedRows.Count == 0) return;
            if (gridRequests.SelectedRows[0].Tag is RequestInfo info)
            {
                DisplayHeaders(info);
            }
        }

        private void DisplayHeaders(RequestInfo info)
        {
            gridRequestHeaders.Rows.Clear();
            foreach (var h in info.RequestHeaders)
            {
                gridRequestHeaders.Rows.Add(h.Key, h.Value);
            }
            gridResponseHeaders.Rows.Clear();
            foreach (var h in info.ResponseHeaders)
            {
                gridResponseHeaders.Rows.Add(h.Key, h.Value);
            }

            txtRequestBody.Text = info.RequestBody ?? string.Empty;
            txtResponseBody.Text = info.ResponseBody ?? string.Empty;
        }

        public ProxyForm()
        {
            InitializeComponent();
            _proxy = new ProxyService(Log);
        }

        private async void ProxyForm_Load(object sender, EventArgs e)
        {
            Log("Instalando certificado...");
            await CertificateHelper.InstallRootCertificateAsync(_proxy.Server);
            Log("Iniciando proxy...");
            await _proxy.StartAsync();
        }

        public void AddRequest(RequestInfo info)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddRequest(info)));
                return;
            }

            if (!_domainRequests.TryGetValue(info.Domain, out var list))
            {
                list = new System.Collections.Generic.List<RequestInfo>();
                _domainRequests[info.Domain] = list;
                UpdateDomainList();
            }
            list.Add(info);

            if (listDomains.SelectedItem != null && listDomains.SelectedItem.ToString() == info.Domain)
            {
                _displayRequests.Add(info);
                AddRequestToGrid(info);
            }
        }

        private void listDomains_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listDomains.SelectedItem == null) return;
            string domain = listDomains.SelectedItem.ToString();
            _displayRequests = _domainRequests[domain];
            RefreshGrid();
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void ProxyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_proxyEnabled)
            {
                WindowsProxyHelper.Disable();
            }
            if (_phpIniModified)
            {
                PhpIniHelper.RestorePhpConfiguration();
                _phpIniModified = false;
            }
        }

        private void btnToggleProxy_Click(object sender, EventArgs e)
        {
            if (!_proxyEnabled)
            {
                WindowsProxyHelper.Enable("127.0.0.1", 8080);
                if (checkUpdatePhpIni.Checked)
                {
                    string pem = CertificateHelper.ExportRootCertificatePem(_proxy.Server);
                    PhpIniHelper.ConfigurePhpCertificate(pem);
                    _phpIniModified = true;
                }
                btnToggleProxy.Text = "Desactivar Proxy";
                _proxyEnabled = true;
            }
            else
            {
                WindowsProxyHelper.Disable();
                if (_phpIniModified)
                {
                    PhpIniHelper.RestorePhpConfiguration();
                    _phpIniModified = false;
                }
                btnToggleProxy.Text = "Activar Proxy";
                _proxyEnabled = false;
            }
        }

        private void txtDomainFilter_TextChanged(object sender, EventArgs e)
        {
            _domainFilter = txtDomainFilter.Text.ToLowerInvariant();
            UpdateDomainList();
        }

        private void UpdateDomainList()
        {
            var selected = listDomains.SelectedItem as string;
            listDomains.Items.Clear();
            foreach (var domain in _domainRequests.Keys)
            {
                if (string.IsNullOrEmpty(_domainFilter) || domain.ToLowerInvariant().Contains(_domainFilter))
                {
                    listDomains.Items.Add(domain);
                }
            }
            if (selected != null && listDomains.Items.Contains(selected))
            {
                listDomains.SelectedItem = selected;
            }
        }

        private bool FilterRequest(RequestInfo info)
        {
            bool methodOk = _methodFilter == "All" || info.Method.Equals(_methodFilter, StringComparison.OrdinalIgnoreCase);
            bool statusOk = _statusFilter == "All" || info.StatusCode.ToString() == _statusFilter;
            return methodOk && statusOk;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            _methodFilter = comboMethodFilter.SelectedItem?.ToString() ?? "All";
            _statusFilter = comboStatusFilter.SelectedItem?.ToString() ?? "All";
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (listDomains.SelectedItem == null) return;
            gridRequests.Rows.Clear();
            foreach (var r in _domainRequests[listDomains.SelectedItem.ToString()])
            {
                AddRequestToGrid(r);
            }
        }

        private void gridRequests_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridRequests.Columns[e.ColumnIndex].Name == "ColumnActions")
            {
                if (gridRequests.Rows[e.RowIndex].Tag is RequestInfo info)
                {
                    using var sfd = new SaveFileDialog();
                    sfd.Filter = "HTTP Request|*.txt|All files|*.*";
                    sfd.FileName = "request.txt";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"{info.Method} {info.Url} HTTP/1.1");
                        foreach (var h in info.RequestHeaders)
                        {
                            sb.AppendLine($"{h.Key}: {h.Value}");
                        }
                        sb.AppendLine();
                        if (!string.IsNullOrEmpty(info.RequestBody))
                        {
                            sb.AppendLine(info.RequestBody);
                        }

                        sb.AppendLine();
                        sb.AppendLine($"HTTP/1.1 {info.StatusCode}");
                        foreach (var h in info.ResponseHeaders)
                        {
                            sb.AppendLine($"{h.Key}: {h.Value}");
                        }
                        sb.AppendLine();
                        if (!string.IsNullOrEmpty(info.ResponseBody))
                        {
                            sb.AppendLine(info.ResponseBody);
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString());
                    }
                }
            }
        }
    }
}