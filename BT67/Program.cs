using System.Drawing;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new frmPowerGridMonitor());
    }
}

public class frmPowerGridMonitor : Form
{
    readonly ComboBox cboStation = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    readonly Button btnStart = new() { Text = "Bắt đầu Giám sát SCADA", AutoSize = true };
    readonly Button btnStop = new() { Text = "Ngắt kết nối", AutoSize = true, Enabled = false };
    readonly Label lblVoltage = new() { Text = "-- kV", AutoSize = true, Font = new Font("Segoe UI", 32, FontStyle.Bold) };
    readonly Label lblLoadPercent = new() { Text = "-- %", AutoSize = true, Font = new Font("Segoe UI", 32, FontStyle.Bold) };
    readonly Label lblWarning = new() { Text = "Chưa kết nối", AutoSize = true };
    readonly DataGridView dgvGridLogs = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
    readonly ToolStripStatusLabel statusTime = new("Gói cuối: --:--:--");
    readonly ToolStripStatusLabel statusCount = new("Chu kỳ: 0");
    CancellationTokenSource? cts;
    Task? worker;
    int count;
    bool closing;

    public frmPowerGridMonitor()
    {
        Text = "BT6.7 - Trung tâm điều độ và giám sát lưới điện";
        Width = 1000; Height = 650; MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 11);
        cboStation.Items.AddRange(new object[] { "Trạm 110kV Hà Đông", "Trạm 220kV Chèm" });
        cboStation.SelectedIndex = 0;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(16) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 175));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var controls = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        controls.Controls.AddRange(new Control[] { new Label { Text = "Trạm biến áp:", AutoSize = true }, cboStation, btnStart, btnStop });
        root.Controls.Add(controls, 0, 0);
        var dashboard = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dashboard.Controls.Add(MakePanel("Điện áp hiện tại", lblVoltage), 0, 0);
        dashboard.Controls.Add(MakePanel("Mức mang tải", lblLoadPercent, lblWarning), 1, 0);
        root.Controls.Add(dashboard, 0, 1);
        root.Controls.Add(new Label { Text = "NHẬT KÝ SỰ CỐ QUÁ TẢI", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold) }, 0, 2);
        foreach (var heading in new[] { "Thời gian", "Trạm biến áp", "Điện áp (kV)", "Mức tải (%)", "Trạng thái" })
            dgvGridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = heading });
        root.Controls.Add(dgvGridLogs, 0, 3);
        var status = new StatusStrip();
        status.Items.AddRange(new ToolStripItem[] { statusTime, statusCount });
        Controls.Add(root); Controls.Add(status);
        btnStart.Click += StartMonitoring;
        btnStop.Click += async (_, _) => await StopMonitoring();
        FormClosing += OnClosing;
    }

    static Control MakePanel(string heading, params Control[] items)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(15), BackColor = Color.AliceBlue, Margin = new Padding(5) };
        panel.Controls.Add(new Label { Text = heading, AutoSize = true });
        panel.Controls.AddRange(items);
        return panel;
    }

    void StartMonitoring(object? sender, EventArgs e)
    {
        if (cts != null) return;
        string station = cboStation.Text;
        count = 0;
        statusCount.Text = "Chu kỳ: 0";
        statusTime.Text = "Gói cuối: --:--:--";
        lblVoltage.Text = "-- kV";
        lblLoadPercent.Text = "-- %";
        lblLoadPercent.ForeColor = SystemColors.ControlText;
        lblWarning.Text = "Đang kết nối...";
        btnStart.Enabled = false; btnStop.Enabled = true; cboStation.Enabled = false;
        cts = new CancellationTokenSource();
        var token = cts.Token;
        worker = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(2000, token);
                double voltage = Math.Round(108 + Random.Shared.NextDouble() * 7, 2);
                int load = Random.Shared.Next(50, 99);
                DateTime time = DateTime.Now;
                if (!token.IsCancellationRequested) UpdateDashboard(station, voltage, load, time);
            }
        }, token);
    }

    void UpdateDashboard(string station, double voltage, int load, DateTime time)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => UpdateDashboard(station, voltage, load, time)));
            return;
        }
        if (cts == null || cts.IsCancellationRequested) return;
        count++;
        bool overload = load > 85;
        lblVoltage.Text = $"{voltage:F2} kV";
        lblLoadPercent.Text = $"{load}%";
        lblLoadPercent.ForeColor = overload ? Color.Red : Color.Green;
        lblWarning.Text = overload ? "CẢNH BÁO QUÁ TẢI" : "Hoạt động ổn định";
        lblWarning.ForeColor = lblLoadPercent.ForeColor;
        statusTime.Text = $"Gói cuối: {time:HH:mm:ss}";
        statusCount.Text = $"Chu kỳ: {count}";
        if (overload)
        {
            int index = dgvGridLogs.Rows.Add(time.ToString("HH:mm:ss"), station, voltage.ToString("F2"), load, "Quá tải");
            dgvGridLogs.Rows[index].DefaultCellStyle.ForeColor = Color.Red;
            dgvGridLogs.FirstDisplayedScrollingRowIndex = index;
        }
    }

    async Task StopMonitoring()
    {
        if (cts == null) return;
        var source = cts;
        btnStop.Enabled = false;
        source.Cancel();
        try { if (worker != null) await worker; }
        catch (OperationCanceledException) { }
        finally
        {
            if (cts == source)
            {
                source.Dispose(); cts = null; worker = null;
                btnStart.Enabled = true; cboStation.Enabled = true;
                lblWarning.Text = "Đã ngắt kết nối";
                lblWarning.ForeColor = SystemColors.ControlText;
            }
        }
    }

    async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (cts == null) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        await StopMonitoring();
        Close();
    }
}
