using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucHoaDon : UserControl
    {
        private readonly HoaDonService _service = new();
        private readonly EmailService _emailService = new();
        private DataGridView dgv = null!;
        private ComboBox cboTrangThai = null!;
        private DateTimePicker dtpThang = null!;
        private HoaDon? _selected;

        public ucHoaDon()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeManager.Surface, Padding = new Padding(15, 10, 15, 10) };

            var btnCreate = CreateButton("➕ Tạo HĐ", ThemeManager.Primary, 15);
            btnCreate.Click += BtnCreate_Click;

            var btnBatch = CreateButton("📦 Tạo hàng loạt", Color.FromArgb(168, 85, 247), 115);
            btnBatch.Width = 120;
            btnBatch.Click += BtnBatch_Click;

            var btnPay = CreateButton("💰 Thanh toán", Color.FromArgb(34, 197, 94), 245);
            btnPay.Width = 110;
            btnPay.Click += BtnPay_Click;

            var btnEmail = CreateButton("📧 Gửi email", Color.FromArgb(236, 72, 153), 365);
            btnEmail.Width = 100;
            btnEmail.Click += BtnEmail_Click;

            var lblThang = new Label { Text = "Tháng:", Location = new Point(490, 18), AutoSize = true };
            dtpThang = new DateTimePicker { Location = new Point(540, 14), Size = new Size(100, 25), Format = DateTimePickerFormat.Custom, CustomFormat = "MM/yyyy", ShowUpDown = true };
            dtpThang.ValueChanged += (s, e) => LoadData();

            var lblFilter = new Label { Text = "TT:", Location = new Point(660, 18), AutoSize = true };
            cboTrangThai = new ComboBox { Location = new Point(690, 14), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrangThai.Items.AddRange(new object[] { "-- Tất cả --", "ChuaThanhToan", "DaThanhToan", "QuaHan" });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { btnCreate, btnBatch, btnPay, btnEmail, lblThang, dtpThang, lblFilter, cboTrangThai });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            SetupColumns();
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is HoaDon h) _selected = h; };
            dgv.CellFormatting += Dgv_CellFormatting;

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateButton(string text, Color color, int x)
        {
            return new Button { Text = text, Size = new Size(90, 35), Location = new Point(x, 12), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand };
        }

        private void SetupColumns()
        {
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaHoaDon", "Mã HĐ", "MaHoaDon", 100);
            UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 70);
            UIHelper.AddColumn(dgv, "TenKhachThue", "Khách thuê", "TenKhachThue", 130);
            UIHelper.AddColumn(dgv, "ThangNam", "Tháng", "ThangNam", 80);
            UIHelper.AddColumn(dgv, "TienPhong", "Tiền phòng", "TienPhong", 100);
            UIHelper.AddColumn(dgv, "TongTienDichVu", "Dịch vụ", "TongTienDichVu", 90);
            UIHelper.AddColumn(dgv, "TongCong", "Tổng cộng", "TongCong", 100);
            UIHelper.AddColumn(dgv, "DaThanhToan", "Đã TT", "DaThanhToan", 90);
            UIHelper.AddColumn(dgv, "ConNo", "Còn nợ", "ConNo", 90);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 100);
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            var colName = dgv.Columns[e.ColumnIndex].Name;
            if (colName == "TrangThai" && e.Value != null)
            {
                e.CellStyle!.ForeColor = UIHelper.GetStatusColor(e.Value.ToString()!);
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            }
            if ((colName == "TienPhong" || colName == "TongTienDichVu" || colName == "TongCong" || colName == "DaThanhToan" || colName == "ConNo") && e.Value != null)
                e.Value = string.Format("{0:N0}", e.Value);
            if (colName == "ThangNam" && e.Value != null)
                e.Value = ((DateTime)e.Value).ToString("MM/yyyy");
        }

        private async void LoadData()
        {
            try
            {
                string? trangThai = cboTrangThai.SelectedIndex > 0 ? cboTrangThai.Text : null;
                var data = await _service.GetAllAsync(trangThai, dtpThang.Value.Year, dtpThang.Value.Month);
                dgv.DataSource = data.ToList();
            }
            catch (Exception ex) { UIHelper.ShowError(ex.Message); }
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            var hopDongService = new HopDongService();
            var dichVuService = new DichVuService();
            var hopDongs = await hopDongService.GetAllAsync("Active");
            var dichVus = await dichVuService.GetAllAsync();

            using var frm = new frmHoaDonCreate(hopDongs, dichVus);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnBatch_Click(object? sender, EventArgs e)
        {
            if (!UIHelper.Confirm($"Tạo hóa đơn hàng loạt cho tháng {dtpThang.Value:MM/yyyy}?")) return;

            var (success, failed, msg) = await _service.CreateBatchAsync(dtpThang.Value);
            UIHelper.ShowSuccess(msg);
            LoadData();
        }

        private async void BtnPay_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn hóa đơn!"); return; }
            if (_selected.TrangThai == "DaThanhToan") { UIHelper.ShowWarning("Hóa đơn đã thanh toán!"); return; }

            using var frm = new frmPayment(_selected);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnEmail_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn hóa đơn!"); return; }

            var (ok, msg) = await _emailService.SendInvoiceAsync(_selected.HoaDonId);
            if (ok) UIHelper.ShowSuccess(msg);
            else UIHelper.ShowError(msg);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucHoaDon";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }

    public class frmHoaDonCreate : Form
    {
        private readonly HoaDonService _service = new();
        private ComboBox cboHopDong = null!;
        private DateTimePicker dtpThang = null!;
        private DataGridView dgvDichVu = null!;
        private Label lblTong = null!;
        private IEnumerable<DichVu> _dichVus;
        private IEnumerable<HopDong> _hopDongs;

        public frmHoaDonCreate(IEnumerable<HopDong> hopDongs, IEnumerable<DichVu> dichVus)
        {
            _hopDongs = hopDongs;
            _dichVus = dichVus;
            SetupForm();
            CreateControls();
        }

        private void SetupForm()
        {
            this.Text = "Tạo hóa đơn";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            var lblHD = new Label { Text = "Hợp đồng:", Location = new Point(20, 20), AutoSize = true };
            cboHopDong = new ComboBox
            {
                Location = new Point(100, 17),
                Size = new Size(460, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = _hopDongs.Select(h => new { h.HopDongId, Display = $"{h.MaHopDong} - {h.MaPhong} - {h.TenKhachThue} ({h.GiaThue:N0})" }).ToList(),
                DisplayMember = "Display",
                ValueMember = "HopDongId"
            };

            var lblThang = new Label { Text = "Tháng:", Location = new Point(20, 55), AutoSize = true };
            dtpThang = new DateTimePicker { Location = new Point(100, 52), Size = new Size(150, 25), Format = DateTimePickerFormat.Custom, CustomFormat = "MM/yyyy", ShowUpDown = true };

            var lblDV = new Label { Text = "Chi tiết dịch vụ:", Location = new Point(20, 90), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            dgvDichVu = new DataGridView
            {
                Location = new Point(20, 115),
                Size = new Size(540, 280),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            dgvDichVu.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "Chọn", Width = 50 });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenDichVu", HeaderText = "Dịch vụ", Width = 150, ReadOnly = true });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonGia", HeaderText = "Đơn giá", Width = 80, ReadOnly = true });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChiSoCu", HeaderText = "Chỉ số cũ", Width = 80 });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChiSoMoi", HeaderText = "Chỉ số mới", Width = 80 });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThanhTien", HeaderText = "Thành tiền", Width = 90, ReadOnly = true });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "DichVuId", Visible = false });
            dgvDichVu.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoaiDichVu", Visible = false });

            foreach (var dv in _dichVus)
            {
                dgvDichVu.Rows.Add(true, dv.TenDichVu, dv.DonGia, 0, 0, dv.LoaiDichVu == "CoDinh" ? dv.DonGia : 0, dv.DichVuId, dv.LoaiDichVu);
            }

            dgvDichVu.CellEndEdit += (s, e) => CalculateTotal();

            lblTong = new Label { Text = "Tổng tiền dịch vụ: 0 VNĐ", Location = new Point(20, 405), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            var btnSave = new Button { Text = "💾 Tạo hóa đơn", Location = new Point(180, 450), Size = new Size(130, 40), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(320, 450), Size = new Size(100, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { lblHD, cboHopDong, lblThang, dtpThang, lblDV, dgvDichVu, lblTong, btnSave, btnCancel });
        }

        private void CalculateTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvDichVu.Rows)
            {
                if (row.Cells["Chon"].Value is true)
                {
                    var loai = row.Cells["LoaiDichVu"].Value?.ToString();
                    var donGia = decimal.TryParse(row.Cells["DonGia"].Value?.ToString(), out var dg) ? dg : 0;

                    if (loai == "TheoChiSo")
                    {
                        var cu = decimal.TryParse(row.Cells["ChiSoCu"].Value?.ToString(), out var c) ? c : 0;
                        var moi = decimal.TryParse(row.Cells["ChiSoMoi"].Value?.ToString(), out var m) ? m : 0;
                        var thanhTien = (moi - cu) * donGia;
                        row.Cells["ThanhTien"].Value = thanhTien;
                        total += thanhTien;
                    }
                    else
                    {
                        row.Cells["ThanhTien"].Value = donGia;
                        total += donGia;
                    }
                }
            }
            lblTong.Text = $"Tổng tiền dịch vụ: {total:N0} VNĐ";
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var hopDongId = (int)cboHopDong.SelectedValue!;
            var chiTiet = new List<ChiTietHoaDon>();

            foreach (DataGridViewRow row in dgvDichVu.Rows)
            {
                if (row.Cells["Chon"].Value is true)
                {
                    chiTiet.Add(new ChiTietHoaDon
                    {
                        DichVuId = int.Parse(row.Cells["DichVuId"].Value?.ToString() ?? "0"),
                        ChiSoCu = decimal.TryParse(row.Cells["ChiSoCu"].Value?.ToString(), out var cu) ? cu : null,
                        ChiSoMoi = decimal.TryParse(row.Cells["ChiSoMoi"].Value?.ToString(), out var moi) ? moi : null,
                        DonGia = decimal.Parse(row.Cells["DonGia"].Value?.ToString() ?? "0"),
                        ThanhTien = decimal.Parse(row.Cells["ThanhTien"].Value?.ToString() ?? "0")
                    });
                }
            }

            var (ok, msg, _) = await _service.CreateAsync(hopDongId, dtpThang.Value, chiTiet);
            if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
            else UIHelper.ShowError(msg);
        }
    }

    public class frmPayment : Form
    {
        private readonly HoaDonService _service = new();
        private readonly HoaDon _hoaDon;
        private NumericUpDown nudSoTien = null!;

        public frmPayment(HoaDon hoaDon)
        {
            _hoaDon = hoaDon;
            SetupForm();
            CreateControls();
        }

        private void SetupForm()
        {
            this.Text = "Thanh toán hóa đơn";
            this.Size = new Size(400, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            var lblInfo = new Label
            {
                Text = $"Hóa đơn: {_hoaDon.MaHoaDon}\nPhòng: {_hoaDon.MaPhong}\nTổng cộng: {_hoaDon.TongCong:N0} VNĐ\nĐã thanh toán: {_hoaDon.DaThanhToan:N0} VNĐ\nCòn nợ: {_hoaDon.ConNo:N0} VNĐ",
                Location = new Point(20, 20),
                Size = new Size(350, 100)
            };

            var lblSoTien = new Label { Text = "Số tiền thanh toán:", Location = new Point(20, 130), AutoSize = true };
            nudSoTien = new NumericUpDown { Location = new Point(150, 127), Size = new Size(200, 25), Maximum = _hoaDon.ConNo, Value = _hoaDon.ConNo, ThousandsSeparator = true };

            var btnPay = new Button { Text = "💰 Thanh toán", Location = new Point(100, 180), Size = new Size(120, 40), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnPay.Click += async (s, e) =>
            {
                var (ok, msg) = await _service.PaymentAsync(_hoaDon.HoaDonId, nudSoTien.Value);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(230, 180), Size = new Size(80, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { lblInfo, lblSoTien, nudSoTien, btnPay, btnCancel });
        }
    }
}
