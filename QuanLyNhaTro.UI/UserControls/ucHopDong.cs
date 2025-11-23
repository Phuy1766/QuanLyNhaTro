using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucHopDong : UserControl
    {
        private readonly HopDongService _service = new();
        private readonly BackgroundTaskService _backgroundService = new();
        private DataGridView dgv = null!;
        private ComboBox cboTrangThai = null!;
        private HopDong? _selected;

        public ucHopDong()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeManager.Surface, Padding = new Padding(15, 10, 15, 10) };

            var btnAdd = CreateButton("➕ Tạo HĐ", ThemeManager.Primary, 15);
            btnAdd.Click += BtnAdd_Click;

            var btnExtend = CreateButton("📅 Gia hạn", Color.FromArgb(34, 197, 94), 125);
            btnExtend.Click += BtnExtend_Click;

            var btnTerminate = CreateButton("🛑 Thanh lý", Color.FromArgb(239, 68, 68), 235);
            btnTerminate.Click += BtnTerminate_Click;

            var btnAutoExpire = CreateButton("🔄 Cập nhật HĐ", Color.FromArgb(251, 146, 60), 345);
            btnAutoExpire.Click += BtnAutoExpire_Click;
            btnAutoExpire.Size = new Size(125, 35);

            var btnDelete = CreateButton("🗑️ Xóa HĐ", Color.FromArgb(220, 38, 38), 480);
            btnDelete.Click += BtnDelete_Click;
            btnDelete.Size = new Size(95, 35);

            var lblFilter = new Label { Text = "Trạng thái:", Location = new Point(595, 18), AutoSize = true };
            cboTrangThai = new ComboBox { Location = new Point(665, 14), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrangThai.Items.AddRange(new object[] { "-- Tất cả --", "Active", "Expired", "Terminated" });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnExtend, btnTerminate, btnAutoExpire, btnDelete, lblFilter, cboTrangThai });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            SetupColumns();
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is HopDong h) _selected = h; };
            dgv.CellFormatting += Dgv_CellFormatting;

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateButton(string text, Color color, int x)
        {
            return new Button { Text = text, Size = new Size(100, 35), Location = new Point(x, 12), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand };
        }

        private void SetupColumns()
        {
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaHopDong", "Mã HĐ", "MaHopDong", 100);
            UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 80);
            UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 100);
            UIHelper.AddColumn(dgv, "TenKhachThue", "Khách thuê", "TenKhachThue", 150);
            UIHelper.AddColumn(dgv, "NgayBatDau", "Ngày BĐ", "NgayBatDau", 90);
            UIHelper.AddColumn(dgv, "NgayKetThuc", "Ngày KT", "NgayKetThuc", 90);
            UIHelper.AddColumn(dgv, "GiaThue", "Giá thuê", "GiaThue", 100);
            UIHelper.AddColumn(dgv, "TienCoc", "Tiền cọc", "TienCoc", 100);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 90);
            UIHelper.AddColumn(dgv, "SoNgayConLai", "Còn lại", "SoNgayConLai", 70);
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            var colName = dgv.Columns[e.ColumnIndex].Name;
            if (colName == "TrangThai" && e.Value != null)
            {
                e.CellStyle!.ForeColor = UIHelper.GetStatusColor(e.Value.ToString()!);
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            }
            if ((colName == "GiaThue" || colName == "TienCoc") && e.Value != null)
                e.Value = string.Format("{0:N0}", e.Value);
            if ((colName == "NgayBatDau" || colName == "NgayKetThuc") && e.Value != null)
                e.Value = ((DateTime)e.Value).ToString("dd/MM/yyyy");
        }

        private async void LoadData()
        {
            try
            {
                string? trangThai = cboTrangThai.SelectedIndex > 0 ? cboTrangThai.Text : null;
                var data = await _service.GetAllAsync(trangThai);
                dgv.DataSource = data.ToList();
            }
            catch (Exception ex) { UIHelper.ShowError(ex.Message); }
        }

        private async void BtnAutoExpire_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!UIHelper.Confirm("Cập nhật trạng thái hợp đồng hết hạn và trả phòng về trống?\n\nThao tác này sẽ:\n• Chuyển hợp đồng hết hạn sang Expired\n• Cập nhật phòng về trạng thái Trống"))
                    return;

                var result = await _backgroundService.AutoExpireContractsAsync();
                
                if (result.ExpiredCount > 0)
                {
                    UIHelper.ShowSuccess($"Đã cập nhật:\n• {result.ExpiredCount} hợp đồng → Expired\n• {result.UpdatedRooms} phòng → Trống");
                    LoadData();
                }
                else
                {
                    UIHelper.ShowWarning("Không có hợp đồng nào hết hạn cần cập nhật.");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi cập nhật: {ex.Message}");
            }
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            var phongService = new PhongTroService();
            var khachService = new KhachThueService();
            var phongs = await phongService.GetAvailableAsync();
            var khachs = await khachService.GetAvailableAsync();

            using var frm = new frmHopDongEdit(phongs, khachs);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnExtend_Click(object? sender, EventArgs e)
        {
            if (_selected == null || _selected.TrangThai != "Active")
            {
                UIHelper.ShowWarning("Vui lòng chọn hợp đồng đang hoạt động!");
                return;
            }

                try
                {
                    using var frm = new frmExtendContract(_selected);
                    if (frm.ShowDialog() == DialogResult.OK) await Task.Run(LoadData);
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi gia hạn hợp đồng: {ex.Message}");
                }
        }

        private async void BtnTerminate_Click(object? sender, EventArgs e)
        {
            if (_selected == null || _selected.TrangThai != "Active")
            {
                UIHelper.ShowWarning("Vui lòng chọn hợp đồng đang hoạt động!");
                return;
            }

                try
                {
                    using var frm = new frmTerminateContract(_selected);
                    if (frm.ShowDialog() == DialogResult.OK) await Task.Run(LoadData);
                }
                catch (Exception ex)
                {
                    UIHelper.ShowError($"Lỗi chấm dứt hợp đồng: {ex.Message}");
                }
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selected == null)
            {
                UIHelper.ShowWarning("Vui lòng chọn hợp đồng!");
                return;
            }

            if (_selected.TrangThai != "Expired")
            {
                UIHelper.ShowWarning("Chỉ có thể xóa hợp đồng đã hết hạn!");
                return;
            }

            if (!UIHelper.Confirm($"Bạn có chắc muốn xóa hợp đồng {_selected.MaHopDong}?\n\nLưu ý: Chỉ xóa được hợp đồng không còn hóa đơn chưa thanh toán."))
                return;

            try
            {
                var (success, message) = await _service.DeleteExpiredAsync(_selected.HopDongId);
                
                if (success)
                {
                    UIHelper.ShowSuccess(message);
                    LoadData();
                }
                else
                {
                    UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi xóa hợp đồng: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucHopDong";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }

    public class frmHopDongEdit : Form
    {
        private readonly HopDongService _service = new();
        private ComboBox cboPhong = null!, cboKhach = null!;
        private DateTimePicker dtpBatDau = null!, dtpKetThuc = null!;
        private NumericUpDown nudGiaThue = null!, nudTienCoc = null!;

        public frmHopDongEdit(IEnumerable<PhongTro> phongs, IEnumerable<KhachThue> khachs)
        {
            SetupForm();
            CreateControls(phongs, khachs);
        }

        private void SetupForm()
        {
            this.Text = "Tạo hợp đồng mới";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls(IEnumerable<PhongTro> phongs, IEnumerable<KhachThue> khachs)
        {
            int y = 20;
            AddLabel("Phòng:", y);
            cboPhong = new ComboBox { Location = new Point(130, y), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboPhong.DataSource = phongs.Select(p => new { p.PhongId, Display = $"{p.MaPhong} - {p.BuildingName} ({p.GiaThue:N0})" }).ToList();
            cboPhong.DisplayMember = "Display";
            cboPhong.ValueMember = "PhongId";
            cboPhong.SelectedIndexChanged += (s, e) =>
            {
                var p = phongs.FirstOrDefault(x => x.PhongId == (int)cboPhong.SelectedValue!);
                if (p != null) nudGiaThue.Value = p.GiaThue;
            };
            this.Controls.Add(cboPhong); y += 45;

            AddLabel("Khách thuê:", y);
            cboKhach = new ComboBox { Location = new Point(130, y), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboKhach.DataSource = khachs.Select(k => new { k.KhachId, Display = $"{k.MaKhach} - {k.HoTen}" }).ToList();
            cboKhach.DisplayMember = "Display";
            cboKhach.ValueMember = "KhachId";
            this.Controls.Add(cboKhach); y += 45;

            AddLabel("Ngày bắt đầu:", y);
            dtpBatDau = new DateTimePicker { Location = new Point(130, y), Size = new Size(320, 25), Format = DateTimePickerFormat.Short };
            this.Controls.Add(dtpBatDau); y += 45;

            AddLabel("Ngày kết thúc:", y);
            dtpKetThuc = new DateTimePicker { Location = new Point(130, y), Size = new Size(320, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(1) };
            this.Controls.Add(dtpKetThuc); y += 45;

            AddLabel("Giá thuê:", y);
            nudGiaThue = new NumericUpDown { Location = new Point(130, y), Size = new Size(320, 25), Maximum = 100000000, ThousandsSeparator = true };
            this.Controls.Add(nudGiaThue); y += 45;

            AddLabel("Tiền cọc:", y);
            nudTienCoc = new NumericUpDown { Location = new Point(130, y), Size = new Size(320, 25), Maximum = 100000000, ThousandsSeparator = true };
            this.Controls.Add(nudTienCoc); y += 55;

            var btnSave = new Button { Text = "💾 Tạo hợp đồng", Location = new Point(130, y), Size = new Size(140, 40), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(280, y), Size = new Size(100, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddLabel(string text, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 5), AutoSize = true });

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var hopDong = new HopDong
            {
                PhongId = (int)cboPhong.SelectedValue!,
                KhachId = (int)cboKhach.SelectedValue!,
                NgayBatDau = dtpBatDau.Value,
                NgayKetThuc = dtpKetThuc.Value,
                GiaThue = nudGiaThue.Value,
                TienCoc = nudTienCoc.Value
            };

            var (ok, msg, _) = await _service.CreateAsync(hopDong);
            if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
            else UIHelper.ShowError(msg);
        }
    }

    public class frmExtendContract : Form
    {
        private readonly HopDongService _service = new();
        private readonly HopDong _hopDong;
        private DateTimePicker dtpKetThuc = null!;
        private NumericUpDown nudGiaMoi = null!;

        public frmExtendContract(HopDong hopDong)
        {
            _hopDong = hopDong;
            SetupForm();
            CreateControls();
        }

        private void SetupForm()
        {
            this.Text = "Gia hạn hợp đồng";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            var lblInfo = new Label { Text = $"Hợp đồng: {_hopDong.MaHopDong}\nPhòng: {_hopDong.MaPhong}\nHết hạn hiện tại: {_hopDong.NgayKetThuc:dd/MM/yyyy}", Location = new Point(20, 20), Size = new Size(350, 50) };
            this.Controls.Add(lblInfo);

            var lblNew = new Label { Text = "Ngày kết thúc mới:", Location = new Point(20, 85), AutoSize = true };
            dtpKetThuc = new DateTimePicker { Location = new Point(150, 82), Size = new Size(200, 25), Format = DateTimePickerFormat.Short, Value = _hopDong.NgayKetThuc.AddYears(1) };

            var lblGia = new Label { Text = "Giá thuê mới:", Location = new Point(20, 120), AutoSize = true };
            nudGiaMoi = new NumericUpDown { Location = new Point(150, 117), Size = new Size(200, 25), Maximum = 100000000, Value = _hopDong.GiaThue, ThousandsSeparator = true };

            var btnSave = new Button { Text = "✅ Gia hạn", Location = new Point(100, 160), Size = new Size(100, 35), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += async (s, e) =>
            {
                var (ok, msg) = await _service.ExtendAsync(_hopDong.HopDongId, dtpKetThuc.Value, nudGiaMoi.Value);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(210, 160), Size = new Size(80, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { lblNew, dtpKetThuc, lblGia, nudGiaMoi, btnSave, btnCancel });
        }
    }

    public class frmTerminateContract : Form
    {
        private readonly HopDongService _service = new();
        private readonly HopDong _hopDong;
        private Label lblTienCoc = null!, lblCongNo = null!, lblHuHong = null!, lblTongKhauTru = null!, lblHoanCoc = null!;
        private TextBox txtLyDo = null!;
        private TerminationResult? _result;

        public frmTerminateContract(HopDong hopDong)
        {
            _hopDong = hopDong;
            SetupForm();
            CreateControls();
            LoadCalculation();
        }

        private void SetupForm()
        {
            this.Text = "Thanh lý hợp đồng";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            var lblInfo = new Label { Text = $"Hợp đồng: {_hopDong.MaHopDong} | Phòng: {_hopDong.MaPhong}", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            this.Controls.Add(lblInfo);

            int y = 60;
            AddRow("Tiền cọc:", ref lblTienCoc, y); y += 30;
            AddRow("Công nợ hóa đơn:", ref lblCongNo, y); y += 30;
            AddRow("Chi phí hư hỏng:", ref lblHuHong, y); y += 30;
            AddRow("Tổng khấu trừ:", ref lblTongKhauTru, y, Color.Red); y += 30;
            AddRow("HOÀN CỌC:", ref lblHoanCoc, y, Color.Green, true); y += 45;

            var lblLyDo = new Label { Text = "Lý do khấu trừ:", Location = new Point(20, y), AutoSize = true };
            txtLyDo = new TextBox { Location = new Point(130, y), Size = new Size(280, 60), Multiline = true };
            this.Controls.AddRange(new Control[] { lblLyDo, txtLyDo });
            y += 75;

            var btnConfirm = new Button { Text = "✅ Xác nhận thanh lý", Location = new Point(100, y), Size = new Size(150, 40), BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnConfirm.Click += BtnConfirm_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(260, y), Size = new Size(80, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnConfirm, btnCancel });
        }

        private void AddRow(string text, ref Label lbl, int y, Color? color = null, bool bold = false)
        {
            var lblTitle = new Label { Text = text, Location = new Point(20, y), AutoSize = true };
            lbl = new Label { Text = "0 VNĐ", Location = new Point(200, y), AutoSize = true };
            if (color.HasValue) lbl.ForeColor = color.Value;
            if (bold) lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            this.Controls.AddRange(new Control[] { lblTitle, lbl });
        }

        private async void LoadCalculation()
        {
            var (ok, _, result) = await _service.TerminateAsync(_hopDong.HopDongId);
            if (ok && result != null)
            {
                _result = result;
                lblTienCoc.Text = $"{result.TienCoc:N0} VNĐ";
                lblCongNo.Text = $"{result.CongNoHoaDon:N0} VNĐ";
                lblHuHong.Text = $"{result.ChiPhiHuHong:N0} VNĐ";
                lblTongKhauTru.Text = $"{result.TongKhauTru:N0} VNĐ";
                lblHoanCoc.Text = $"{result.TienHoanCoc:N0} VNĐ";
            }
        }

        private async void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (_result == null) return;
            if (!UIHelper.Confirm("Bạn có chắc muốn thanh lý hợp đồng này?")) return;

            var (ok, msg) = await _service.ConfirmTerminateAsync(_hopDong.HopDongId, _result.TienHoanCoc, _result.TongKhauTru, txtLyDo.Text);
            if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
            else UIHelper.ShowError(msg);
        }
    }
}
