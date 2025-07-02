using System;
using System.Windows.Forms;

namespace ProxyGuy.WinForms
{
    partial class ProxyForm
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridRequests;
        private DataGridView gridRequestHeaders;
        private DataGridView gridResponseHeaders;
        private TextBox txtRequestBody;
        private TextBox txtResponseBody;
        private TextBox txtDomainFilter;
        private ComboBox comboMethodFilter;
        private ComboBox comboStatusFilter;
        private Button btnToggleProxy;
        private Button btnClearLogs;
        private CheckBox checkUpdatePhpIni;
        private ListBox listDomains;
        private TabControl tabsHeaders;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridRequests = new System.Windows.Forms.DataGridView();
            this.gridRequestHeaders = new System.Windows.Forms.DataGridView();
            this.gridResponseHeaders = new System.Windows.Forms.DataGridView();
            this.txtRequestBody = new System.Windows.Forms.TextBox();
            this.txtResponseBody = new System.Windows.Forms.TextBox();
            this.txtDomainFilter = new System.Windows.Forms.TextBox();
            this.comboMethodFilter = new System.Windows.Forms.ComboBox();
            this.comboStatusFilter = new System.Windows.Forms.ComboBox();
            this.btnToggleProxy = new System.Windows.Forms.Button();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.checkUpdatePhpIni = new System.Windows.Forms.CheckBox();
            this.listDomains = new System.Windows.Forms.ListBox();
            this.tabsHeaders = new System.Windows.Forms.TabControl();
            var tabPageRequest = new System.Windows.Forms.TabPage();
            var tabPageResponse = new System.Windows.Forms.TabPage();
            var tabPageRequestBody = new System.Windows.Forms.TabPage();
            var tabPageResponseBody = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequestHeaders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridResponseHeaders)).BeginInit();
            this.SuspendLayout();
            // 
            // gridRequests
            // 
            this.gridRequests.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right))));
            this.gridRequests.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridRequests.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { HeaderText = "Hora", Name = "ColumnTime", Width = 80 },
                new DataGridViewTextBoxColumn { HeaderText = "Método", Name = "ColumnMethod", Width = 70 },
                new DataGridViewTextBoxColumn { HeaderText = "URL", Name = "ColumnUrl", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn { HeaderText = "Status", Name = "ColumnStatus", Width = 60 }
            });
            this.gridRequests.Location = new System.Drawing.Point(200, 35);
            this.gridRequests.Name = "gridRequests";
            this.gridRequests.Size = new System.Drawing.Size(588, 215);
            this.gridRequests.TabIndex = 0;
            this.gridRequests.SelectionChanged += new System.EventHandler(this.gridRequests_SelectionChanged);

            //
            // txtDomainFilter
            //
            this.txtDomainFilter.Location = new System.Drawing.Point(0, 6);
            this.txtDomainFilter.Name = "txtDomainFilter";
            this.txtDomainFilter.Size = new System.Drawing.Size(194, 20);
            this.txtDomainFilter.TabIndex = 1;
            this.txtDomainFilter.TextChanged += new System.EventHandler(this.txtDomainFilter_TextChanged);

            // listDomains
            //
            this.listDomains.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left)));
            this.listDomains.FormattingEnabled = true;
            this.listDomains.Location = new System.Drawing.Point(0, 35);
            this.listDomains.Name = "listDomains";
            this.listDomains.Size = new System.Drawing.Size(194, 407);
            this.listDomains.TabIndex = 3;
            this.listDomains.SelectedIndexChanged += new System.EventHandler(this.listDomains_SelectedIndexChanged);

            //
            // tabsHeaders
            //
            this.tabsHeaders.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.tabsHeaders.Location = new System.Drawing.Point(200, 256);
            this.tabsHeaders.Name = "tabsHeaders";
            this.tabsHeaders.Size = new System.Drawing.Size(588, 194);
            this.tabsHeaders.TabIndex = 4;

            tabPageRequest.Text = "Request";
            tabPageRequest.Controls.Add(this.gridRequestHeaders);
            tabPageRequest.Padding = new Padding(3);
            tabPageRequest.Size = new System.Drawing.Size(580, 168);
            tabPageRequest.UseVisualStyleBackColor = true;

            tabPageResponse.Text = "Response";
            tabPageResponse.Controls.Add(this.gridResponseHeaders);
            tabPageResponse.Padding = new Padding(3);
            tabPageResponse.Size = new System.Drawing.Size(580, 168);
            tabPageResponse.UseVisualStyleBackColor = true;

            tabPageRequestBody.Text = "Request Body";
            tabPageRequestBody.Controls.Add(this.txtRequestBody);
            tabPageRequestBody.Padding = new Padding(3);
            tabPageRequestBody.Size = new System.Drawing.Size(580, 168);
            tabPageRequestBody.UseVisualStyleBackColor = true;

            tabPageResponseBody.Text = "Response Body";
            tabPageResponseBody.Controls.Add(this.txtResponseBody);
            tabPageResponseBody.Padding = new Padding(3);
            tabPageResponseBody.Size = new System.Drawing.Size(580, 168);
            tabPageResponseBody.UseVisualStyleBackColor = true;

            this.tabsHeaders.Controls.Add(tabPageRequest);
            this.tabsHeaders.Controls.Add(tabPageResponse);
            this.tabsHeaders.Controls.Add(tabPageRequestBody);
            this.tabsHeaders.Controls.Add(tabPageResponseBody);

            //
            // gridRequestHeaders
            //
            this.gridRequestHeaders.Dock = DockStyle.Fill;
            this.gridRequestHeaders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridRequestHeaders.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { HeaderText = "Header", Name = "ColumnHeaderReq", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "Value", Name = "ColumnValueReq", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
            this.gridRequestHeaders.Name = "gridRequestHeaders";
            this.gridRequestHeaders.TabIndex = 0;

            //
            // gridResponseHeaders
            //
            this.gridResponseHeaders.Dock = DockStyle.Fill;
            this.gridResponseHeaders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridResponseHeaders.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { HeaderText = "Header", Name = "ColumnHeaderRes", Width = 200 },
                new DataGridViewTextBoxColumn { HeaderText = "Value", Name = "ColumnValueRes", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            });
            this.gridResponseHeaders.Name = "gridResponseHeaders";
            this.gridResponseHeaders.TabIndex = 0;

            //
            // txtRequestBody
            //
            this.txtRequestBody.Dock = DockStyle.Fill;
            this.txtRequestBody.Multiline = true;
            this.txtRequestBody.ScrollBars = ScrollBars.Both;
            this.txtRequestBody.Name = "txtRequestBody";
            this.txtRequestBody.ReadOnly = true;

            //
            // txtResponseBody
            //
            this.txtResponseBody.Dock = DockStyle.Fill;
            this.txtResponseBody.Multiline = true;
            this.txtResponseBody.ScrollBars = ScrollBars.Both;
            this.txtResponseBody.Name = "txtResponseBody";
            this.txtResponseBody.ReadOnly = true;
            //
            // btnToggleProxy
            //
            this.btnToggleProxy.Location = new System.Drawing.Point(206, 6);
            this.btnToggleProxy.Name = "btnToggleProxy";
            this.btnToggleProxy.Size = new System.Drawing.Size(120, 23);
            this.btnToggleProxy.TabIndex = 2;
            this.btnToggleProxy.Text = "Activar Proxy";
            this.btnToggleProxy.UseVisualStyleBackColor = true;
            this.btnToggleProxy.Click += new System.EventHandler(this.btnToggleProxy_Click);

            //
            // btnClearLogs
            //
            this.btnClearLogs.Location = new System.Drawing.Point(504, 6);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(120, 23);
            this.btnClearLogs.TabIndex = 6;
            this.btnClearLogs.Text = "Limpiar Logs";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.btnClearLogs_Click);

            //
            // checkUpdatePhpIni
            //
            this.checkUpdatePhpIni.Location = new System.Drawing.Point(630, 8);
            this.checkUpdatePhpIni.Name = "checkUpdatePhpIni";
            this.checkUpdatePhpIni.Size = new System.Drawing.Size(160, 17);
            this.checkUpdatePhpIni.TabIndex = 7;
            this.checkUpdatePhpIni.Text = "Actualizar php.ini";
            this.checkUpdatePhpIni.UseVisualStyleBackColor = true;

            //
            // comboMethodFilter
            //
            this.comboMethodFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboMethodFilter.Items.AddRange(new object[] { "All", "GET", "POST", "PUT", "DELETE", "HEAD", "OPTIONS", "PATCH" });
            this.comboMethodFilter.Location = new System.Drawing.Point(332, 6);
            this.comboMethodFilter.Name = "comboMethodFilter";
            this.comboMethodFilter.Size = new System.Drawing.Size(80, 21);
            this.comboMethodFilter.TabIndex = 4;
            this.comboMethodFilter.SelectedIndexChanged += new System.EventHandler(this.FilterChanged);

            //
            // comboStatusFilter
            //
            this.comboStatusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboStatusFilter.Items.AddRange(new object[] { "All", "200", "301", "302", "400", "401", "403", "404", "500" });
            this.comboStatusFilter.Location = new System.Drawing.Point(418, 6);
            this.comboStatusFilter.Name = "comboStatusFilter";
            this.comboStatusFilter.Size = new System.Drawing.Size(80, 21);
            this.comboStatusFilter.TabIndex = 5;
            this.comboStatusFilter.SelectedIndexChanged += new System.EventHandler(this.FilterChanged);
            //
            // ProxyForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnClearLogs);
            this.Controls.Add(this.checkUpdatePhpIni);
            this.Controls.Add(this.btnToggleProxy);
            this.Controls.Add(this.comboStatusFilter);
            this.Controls.Add(this.comboMethodFilter);
            this.Controls.Add(this.tabsHeaders);
            this.Controls.Add(this.gridRequests);
            this.Controls.Add(this.listDomains);
            this.Controls.Add(this.txtDomainFilter);
            this.Name = "ProxyForm";
            this.Text = "ProxyGuy";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ProxyForm_FormClosing);
            this.Load += new System.EventHandler(this.ProxyForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridRequests)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridRequestHeaders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridResponseHeaders)).EndInit();
            this.ResumeLayout(false);
        }

        public void AddRequestToGrid(RequestInfo info)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddRequestToGrid(info)));
                return;
            }

            if (FilterRequest(info))
            {
                int rowIndex = gridRequests.Rows.Add(info.Time.ToString("HH:mm:ss"), info.Method, info.Url, info.StatusCode);
                gridRequests.Rows[rowIndex].Tag = info;
            }
        }
    }
}
