using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucBaoTri : UserControl
    {
        private readonly BaoTriService _service = new();
        private DataGridView dgv = null!;
        private ComboBox cboTrangThai = null!;
        private BaoTriTicket? _selected;

        public ucBaoTri()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeManager.Surface, Padding = new Padding(15, 10, 15, 10) };
            var btnAdd = new Button { Text = "➕ Tạo yêu cầu", Size = new Size(110, 35), Location = new Point(15, 12), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.Click += BtnAdd_Click;
            var btnProcess = new Button { Text = "🔧 Xử lý", Size = new Size(80, 35), Location = new Point(135, 12), BackColor = Color.FromArgb(234, 179, 8), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnProcess.Click += BtnProcess_Click;
            var btnComplete = new Button { Text = "✅ Hoàn thành", Size = new Size(100, 35), Location = new Point(225, 12), BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnComplete.Click += BtnComplete_Click;

            var lblFilter = new Label { Text = "Trạng thái:", Location = new Point(360, 18), AutoSize = true };
            cboTrangThai = new ComboBox { Location = new Point(430, 14), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrangThai.Items.AddRange(new object[] { "-- Tất cả --", "Mới", "Đang xử lý", "Hoàn thành" });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnProcess, btnComplete, lblFilter, cboTrangThai });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            SetupColumns();
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is BaoTriTicket b) _selected = b; };
            dgv.CellFormatting += Dgv_CellFormatting;

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private void SetupColumns()
        {
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaTicket", "Mã", "MaTicket", 100);
            UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 70);
            UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 100);
            UIHelper.AddColumn(dgv, "TieuDe", "Tiêu đề", "TieuDe", 200);
            UIHelper.AddColumn(dgv, "MucDoUuTien", "Mức độ", "MucDoUuTien", 100);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgv, "NgayTao", "Ngày tạo", "NgayTao", 100);
            UIHelper.AddColumn(dgv, "TenNguoiXuLy", "Người xử lý", "TenNguoiXuLy", 120);
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            var colName = dgv.Columns[e.ColumnIndex].Name;
            if ((colName == "TrangThai" || colName == "MucDoUuTien") && e.Value != null)
            {
                e.CellStyle!.ForeColor = UIHelper.GetStatusColor(e.Value.ToString()!);
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            }
            if (colName == "NgayTao" && e.Value != null)
                e.Value = ((DateTime)e.Value).ToString("dd/MM/yyyy HH:mm");
        }

        private async void LoadData()
        {
            string? trangThai = cboTrangThai.SelectedIndex > 0 ? cboTrangThai.Text : null;
            dgv.DataSource = (await _service.GetAllAsync(trangThai)).ToList();
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            var phongService = new PhongTroService();
            var phongs = await phongService.GetAllAsync();
            using var frm = new frmBaoTriEdit(phongs);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnProcess_Click(object? sender, EventArgs e)
        {
            if (_selected == null || _selected.TrangThai != "Mới") { UIHelper.ShowWarning("Vui lòng chọn yêu cầu mới!"); return; }
            var (ok, msg) = await _service.ProcessAsync(_selected.TicketId);
            if (ok) { UIHelper.ShowSuccess(msg); LoadData(); }
            else UIHelper.ShowError(msg);
        }

        private async void BtnComplete_Click(object? sender, EventArgs e)
        {
            if (_selected == null || _selected.TrangThai == "Hoàn thành") { UIHelper.ShowWarning("Vui lòng chọn yêu cầu đang xử lý!"); return; }
            using var frm = new frmCompleteTicket(_selected);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void InitializeComponent() { this.Name = "ucBaoTri"; this.Size = new Size(1100, 700); }
    }

    public class frmBaoTriEdit : Form
    {
        private readonly BaoTriService _service = new();
        private ComboBox cboPhong = null!, cboMucDo = null!;
        private TextBox txtTieuDe = null!, txtMoTa = null!;

        public frmBaoTriEdit(IEnumerable<PhongTro> phongs)
        {
            this.Text = "Tạo yêu cầu bảo trì";
            this.Size = new Size(450, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
            CreateControls(phongs);
        }

        private void CreateControls(IEnumerable<PhongTro> phongs)
        {
            int y = 20;
            this.Controls.Add(new Label { Text = "Phòng:", Location = new Point(20, y + 5), AutoSize = true });
            cboPhong = new ComboBox
            {
                Location = new Point(120, y),
                Size = new Size(290, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = phongs.Select(p => new { p.PhongId, Display = $"{p.MaPhong} - {p.BuildingName}" }).ToList(),
                DisplayMember = "Display",
                ValueMember = "PhongId"
            };
            this.Controls.Add(cboPhong); y += 40;

            this.Controls.Add(new Label { Text = "Tiêu đề:", Location = new Point(20, y + 5), AutoSize = true });
            txtTieuDe = new TextBox { Location = new Point(120, y), Size = new Size(290, 25) };
            this.Controls.Add(txtTieuDe); y += 40;

            this.Controls.Add(new Label { Text = "Mức độ:", Location = new Point(20, y + 5), AutoSize = true });
            cboMucDo = new ComboBox { Location = new Point(120, y), Size = new Size(290, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboMucDo.Items.AddRange(new object[] { "Thấp", "Trung bình", "Cao", "Khẩn cấp" });
            cboMucDo.SelectedIndex = 1;
            this.Controls.Add(cboMucDo); y += 40;

            this.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(20, y + 5), AutoSize = true });
            txtMoTa = new TextBox { Location = new Point(120, y), Size = new Size(290, 60), Multiline = true };
            this.Controls.Add(txtMoTa); y += 75;

            var btnSave = new Button { Text = "💾 Tạo", Location = new Point(120, y), Size = new Size(100, 35), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += async (s, e) =>
            {
                var ticket = new BaoTriTicket
                {
                    PhongId = (int)cboPhong.SelectedValue!,
                    TieuDe = txtTieuDe.Text.Trim(),
                    MoTa = txtMoTa.Text.Trim(),
                    MucDoUuTien = cboMucDo.Text
                };
                var (ok, msg, _) = await _service.CreateAsync(ticket);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(230, y), Size = new Size(80, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }
    }

    public class frmCompleteTicket : Form
    {
        private readonly BaoTriService _service = new();
        private readonly BaoTriTicket _ticket;
        private TextBox txtKetQua = null!;
        private NumericUpDown nudChiPhi = null!;

        public frmCompleteTicket(BaoTriTicket ticket)
        {
            _ticket = ticket;
            this.Text = "Hoàn thành bảo trì";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
            CreateControls();
        }

        private void CreateControls()
        {
            var lblInfo = new Label { Text = $"Ticket: {_ticket.MaTicket}\n{_ticket.TieuDe}", Location = new Point(20, 20), Size = new Size(350, 40) };
            this.Controls.Add(lblInfo);

            this.Controls.Add(new Label { Text = "Kết quả xử lý:", Location = new Point(20, 70), AutoSize = true });
            txtKetQua = new TextBox { Location = new Point(120, 67), Size = new Size(240, 50), Multiline = true };
            this.Controls.Add(txtKetQua);

            this.Controls.Add(new Label { Text = "Chi phí:", Location = new Point(20, 130), AutoSize = true });
            nudChiPhi = new NumericUpDown { Location = new Point(120, 127), Size = new Size(240, 25), Maximum = 100000000, ThousandsSeparator = true };
            this.Controls.Add(nudChiPhi);

            var btnSave = new Button { Text = "✅ Hoàn thành", Location = new Point(100, 170), Size = new Size(110, 35), BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += async (s, e) =>
            {
                var (ok, msg) = await _service.CompleteAsync(_ticket.TicketId, txtKetQua.Text, nudChiPhi.Value);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(220, 170), Size = new Size(80, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }
    }
}
