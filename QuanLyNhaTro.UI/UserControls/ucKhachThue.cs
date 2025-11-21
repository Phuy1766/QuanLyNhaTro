using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucKhachThue : UserControl
    {
        private readonly KhachThueService _service = new();
        private DataGridView dgv = null!;
        private TextBox txtSearch = null!;
        private KhachThue? _selected;

        public ucKhachThue()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            // Toolbar
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnAdd = CreateButton("➕ Thêm", ThemeManager.Primary, 15);
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = CreateButton("✏ Sửa", Color.FromArgb(234, 179, 8), 115);
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = CreateButton("🗑 Xóa", Color.FromArgb(239, 68, 68), 205);
            btnDelete.Click += BtnDelete_Click;

            txtSearch = new TextBox
            {
                Location = new Point(320, 15),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍 Tìm kiếm..."
            };
            txtSearch.TextChanged += (s, e) => SearchData();

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, txtSearch });

            // DataGridView
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(15)
            };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.Surface,
                BorderStyle = BorderStyle.None
            };
            UIHelper.StyleDataGridView(dgv);
            SetupColumns();

            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow?.DataBoundItem is KhachThue k)
                    _selected = k;
            };

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateButton(string text, Color color, int x)
        {
            return new Button
            {
                Text = text,
                Size = new Size(90, 35),
                Location = new Point(x, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
        }

        private void SetupColumns()
        {
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaKhach", "Mã khách", "MaKhach", 80);
            UIHelper.AddColumn(dgv, "HoTen", "Họ tên", "HoTen", 150);
            UIHelper.AddColumn(dgv, "CCCD", "CCCD", "CCCD", 120);
            UIHelper.AddColumn(dgv, "Phone", "SĐT", "Phone", 100);
            UIHelper.AddColumn(dgv, "Email", "Email", "Email", 150);
            UIHelper.AddColumn(dgv, "NgheNghiep", "Nghề nghiệp", "NgheNghiep", 100);
            UIHelper.AddColumn(dgv, "MaPhong", "Phòng", "MaPhong", 80);
            UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 100);
        }

        private async void LoadData()
        {
            try
            {
                var data = await _service.GetAllAsync();
                dgv.DataSource = data.ToList();
            }
            catch (Exception ex) { UIHelper.ShowError($"Lỗi: {ex.Message}"); }
        }

        private async void SearchData()
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadData();
                return;
            }
            var data = await _service.SearchAsync(keyword);
            dgv.DataSource = data.ToList();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var frm = new frmKhachThueEdit();
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn khách!"); return; }
            using var frm = new frmKhachThueEdit(_selected);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn khách!"); return; }
            if (!UIHelper.Confirm($"Xóa khách '{_selected.HoTen}'?")) return;
            var (success, msg) = await _service.DeleteAsync(_selected.KhachId);
            if (success) { UIHelper.ShowSuccess(msg); LoadData(); }
            else UIHelper.ShowError(msg);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucKhachThue";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }

    public class frmKhachThueEdit : Form
    {
        private readonly KhachThueService _service = new();
        private readonly KhachThue? _khach;
        private TextBox txtMaKhach = null!, txtHoTen = null!, txtCCCD = null!, txtPhone = null!, txtEmail = null!, txtDiaChi = null!, txtNgheNghiep = null!;
        private DateTimePicker dtpNgaySinh = null!;
        private ComboBox cboGioiTinh = null!;

        public frmKhachThueEdit(KhachThue? khach = null)
        {
            _khach = khach;
            SetupForm();
            CreateControls();
            if (_khach != null) LoadData();
            else LoadMaKhach();
        }

        private void SetupForm()
        {
            this.Text = _khach == null ? "Thêm khách thuê" : "Sửa khách thuê";
            this.Size = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            int y = 20;
            AddLabel("Mã khách:", y); txtMaKhach = AddTextBox(y); txtMaKhach.Enabled = false; y += 40;
            AddLabel("Họ tên: *", y); txtHoTen = AddTextBox(y); y += 40;
            AddLabel("CCCD: *", y); txtCCCD = AddTextBox(y); y += 40;
            AddLabel("Ngày sinh:", y);
            dtpNgaySinh = new DateTimePicker { Location = new Point(130, y), Size = new Size(320, 25), Format = DateTimePickerFormat.Short };
            this.Controls.Add(dtpNgaySinh); y += 40;

            AddLabel("Giới tính:", y);
            cboGioiTinh = new ComboBox { Location = new Point(130, y), Size = new Size(320, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            this.Controls.Add(cboGioiTinh); y += 40;

            AddLabel("SĐT:", y); txtPhone = AddTextBox(y); y += 40;
            AddLabel("Email:", y); txtEmail = AddTextBox(y); y += 40;
            AddLabel("Địa chỉ:", y); txtDiaChi = AddTextBox(y); y += 40;
            AddLabel("Nghề nghiệp:", y); txtNgheNghiep = AddTextBox(y); y += 50;

            var btnSave = new Button { Text = "💾 Lưu", Location = new Point(130, y), Size = new Size(100, 40), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(240, y), Size = new Size(100, 40), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddLabel(string text, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 5), AutoSize = true });
        private TextBox AddTextBox(int y) { var t = new TextBox { Location = new Point(130, y), Size = new Size(320, 25) }; this.Controls.Add(t); return t; }

        private async void LoadMaKhach() => txtMaKhach.Text = await _service.GenerateMaKhachAsync();

        private void LoadData()
        {
            if (_khach == null) return;
            txtMaKhach.Text = _khach.MaKhach;
            txtHoTen.Text = _khach.HoTen;
            txtCCCD.Text = _khach.CCCD;
            if (_khach.NgaySinh.HasValue) dtpNgaySinh.Value = _khach.NgaySinh.Value;
            cboGioiTinh.Text = _khach.GioiTinh;
            txtPhone.Text = _khach.Phone;
            txtEmail.Text = _khach.Email;
            txtDiaChi.Text = _khach.DiaChi;
            txtNgheNghiep.Text = _khach.NgheNghiep;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var khach = _khach ?? new KhachThue();
            khach.MaKhach = txtMaKhach.Text.Trim();
            khach.HoTen = txtHoTen.Text.Trim();
            khach.CCCD = txtCCCD.Text.Trim();
            khach.NgaySinh = dtpNgaySinh.Value;
            khach.GioiTinh = cboGioiTinh.Text;
            khach.Phone = txtPhone.Text.Trim();
            khach.Email = txtEmail.Text.Trim();
            khach.DiaChi = txtDiaChi.Text.Trim();
            khach.NgheNghiep = txtNgheNghiep.Text.Trim();

            try
            {
                if (_khach == null)
                {
                    var (ok, msg, _) = await _service.CreateAsync(khach);
                    if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                    else UIHelper.ShowError(msg);
                }
                else
                {
                    var (ok, msg) = await _service.UpdateAsync(khach);
                    if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                    else UIHelper.ShowError(msg);
                }
            }
            catch (Exception ex) { UIHelper.ShowError(ex.Message); }
        }
    }
}
