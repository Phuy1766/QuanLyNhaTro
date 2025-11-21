using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucPhongTro : UserControl
    {
        private readonly PhongTroService _service = new();
        private readonly BuildingService _buildingService = new();
        private DataGridView dgv = null!;
        private ComboBox cboBuilding = null!;
        private ComboBox cboTrangThai = null!;
        private PhongTro? _selectedPhong;

        public ucPhongTro()
        {
            InitializeComponent();
            CreateLayout();
            LoadFilters();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            // Filter bar
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblBuilding = new Label { Text = "Tòa nhà:", Location = new Point(15, 15), AutoSize = true };
            cboBuilding = new ComboBox
            {
                Location = new Point(70, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboBuilding.SelectedIndexChanged += (s, e) => LoadData();

            var lblTrangThai = new Label { Text = "Trạng thái:", Location = new Point(240, 15), AutoSize = true };
            cboTrangThai = new ComboBox
            {
                Location = new Point(310, 12),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrangThai.Items.AddRange(new object[] { "-- Tất cả --", "Trống", "Đang thuê", "Đang sửa" });
            cboTrangThai.SelectedIndex = 0;
            cboTrangThai.SelectedIndexChanged += (s, e) => LoadData();

            pnlFilter.Controls.AddRange(new Control[] { lblBuilding, cboBuilding, lblTrangThai, cboTrangThai });

            // Toolbar
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(15, 10, 15, 10)
            };

            var btnAdd = CreateButton("➕ Thêm phòng", ThemeManager.Primary, 15);
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = CreateButton("✏ Sửa", Color.FromArgb(234, 179, 8), 135);
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = CreateButton("🗑 Xóa", Color.FromArgb(239, 68, 68), 225);
            btnDelete.Click += BtnDelete_Click;

            var btnRefresh = CreateButton("🔄", ThemeManager.Secondary, 315);
            btnRefresh.Size = new Size(40, 35);
            btnRefresh.Click += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });

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
                if (dgv.CurrentRow?.DataBoundItem is PhongTro p)
                    _selectedPhong = p;
            };

            dgv.CellFormatting += Dgv_CellFormatting;

            pnlGrid.Controls.Add(dgv);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlFilter);
        }

        private Button CreateButton(string text, Color color, int x)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(100, 35),
                Location = new Point(x, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetupColumns()
        {
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaPhong", "Mã phòng", "MaPhong", 80);
            UIHelper.AddColumn(dgv, "BuildingName", "Tòa nhà", "BuildingName", 100);
            UIHelper.AddColumn(dgv, "TenLoai", "Loại phòng", "TenLoai", 100);
            UIHelper.AddColumn(dgv, "Tang", "Tầng", "Tang", 60);
            UIHelper.AddColumn(dgv, "DienTich", "Diện tích", "DienTich", 80);
            UIHelper.AddColumn(dgv, "GiaThue", "Giá thuê", "GiaThue", 100);
            UIHelper.AddColumn(dgv, "SoNguoiToiDa", "Max người", "SoNguoiToiDa", 80);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgv, "TenKhachThue", "Khách thuê", "TenKhachThue", 150);
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "TrangThai" && e.Value != null)
            {
                e.CellStyle!.ForeColor = UIHelper.GetStatusColor(e.Value.ToString()!);
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            }
            if (dgv.Columns[e.ColumnIndex].Name == "GiaThue" && e.Value != null)
            {
                e.Value = string.Format("{0:N0}", e.Value);
            }
        }

        private async void LoadFilters()
        {
            var buildings = (await _buildingService.GetAllAsync()).ToList();
            buildings.Insert(0, new Building { BuildingId = 0, BuildingName = "-- Tất cả --" });
            cboBuilding.DataSource = buildings;
            cboBuilding.DisplayMember = "BuildingName";
            cboBuilding.ValueMember = "BuildingId";
        }

        private async void LoadData()
        {
            try
            {
                int? buildingId = cboBuilding.SelectedValue is int id && id > 0 ? id : null;
                string? trangThai = cboTrangThai.SelectedIndex > 0 ? cboTrangThai.Text : null;

                var data = await _service.GetAllAsync(buildingId, trangThai);
                dgv.DataSource = data.ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            var buildings = await _buildingService.GetAllAsync();
            var loaiPhongs = await _service.GetLoaiPhongAsync();

            using var frm = new frmPhongTroEdit(buildings, loaiPhongs);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadData();
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selectedPhong == null)
            {
                UIHelper.ShowWarning("Vui lòng chọn phòng cần sửa!");
                return;
            }

            var buildings = await _buildingService.GetAllAsync();
            var loaiPhongs = await _service.GetLoaiPhongAsync();

            using var frm = new frmPhongTroEdit(buildings, loaiPhongs, _selectedPhong);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadData();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedPhong == null)
            {
                UIHelper.ShowWarning("Vui lòng chọn phòng cần xóa!");
                return;
            }

            if (!UIHelper.Confirm($"Bạn có chắc muốn xóa phòng '{_selectedPhong.MaPhong}'?"))
                return;

            var (success, message) = await _service.DeleteAsync(_selectedPhong.PhongId);
            if (success)
            {
                UIHelper.ShowSuccess(message);
                LoadData();
            }
            else
                UIHelper.ShowError(message);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucPhongTro";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }

    public class frmPhongTroEdit : Form
    {
        private readonly PhongTroService _service = new();
        private readonly PhongTro? _phong;

        private TextBox txtMaPhong = null!;
        private ComboBox cboBuilding = null!;
        private ComboBox cboLoaiPhong = null!;
        private NumericUpDown nudTang = null!;
        private NumericUpDown nudDienTich = null!;
        private NumericUpDown nudGiaThue = null!;
        private NumericUpDown nudSoNguoi = null!;
        private ComboBox cboTrangThai = null!;

        public frmPhongTroEdit(IEnumerable<Building> buildings, IEnumerable<LoaiPhong> loaiPhongs, PhongTro? phong = null)
        {
            _phong = phong;
            SetupForm();
            CreateControls(buildings, loaiPhongs);
            if (_phong != null)
                LoadData();
        }

        private void SetupForm()
        {
            this.Text = _phong == null ? "Thêm phòng" : "Sửa phòng";
            this.Size = new Size(450, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls(IEnumerable<Building> buildings, IEnumerable<LoaiPhong> loaiPhongs)
        {
            int y = 20;

            AddLabel("Mã phòng:", y);
            txtMaPhong = AddTextBox(y); y += 45;

            AddLabel("Tòa nhà:", y);
            cboBuilding = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = buildings.ToList(),
                DisplayMember = "BuildingName",
                ValueMember = "BuildingId"
            };
            this.Controls.Add(cboBuilding);
            y += 45;

            AddLabel("Loại phòng:", y);
            cboLoaiPhong = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = loaiPhongs.ToList(),
                DisplayMember = "TenLoai",
                ValueMember = "LoaiPhongId"
            };
            this.Controls.Add(cboLoaiPhong);
            y += 45;

            AddLabel("Tầng:", y);
            nudTang = AddNumeric(y, 1, 50, 1); y += 45;

            AddLabel("Diện tích (m²):", y);
            nudDienTich = AddNumeric(y, 5, 500, 20); y += 45;

            AddLabel("Giá thuê:", y);
            nudGiaThue = AddNumeric(y, 0, 100000000, 3000000);
            nudGiaThue.ThousandsSeparator = true;
            y += 45;

            AddLabel("Số người tối đa:", y);
            nudSoNguoi = AddNumeric(y, 1, 20, 2); y += 45;

            AddLabel("Trạng thái:", y);
            cboTrangThai = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrangThai.Items.AddRange(new object[] { "Trống", "Đang thuê", "Đang sửa" });
            cboTrangThai.SelectedIndex = 0;
            this.Controls.Add(cboTrangThai);
            y += 55;

            var btnSave = new Button
            {
                Text = "💾 Lưu",
                Location = new Point(130, y),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(240, y),
                Size = new Size(100, 40),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddLabel(string text, int y)
        {
            this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 5), AutoSize = true });
        }

        private TextBox AddTextBox(int y)
        {
            var txt = new TextBox { Location = new Point(130, y), Size = new Size(280, 25) };
            this.Controls.Add(txt);
            return txt;
        }

        private NumericUpDown AddNumeric(int y, decimal min, decimal max, decimal value)
        {
            var nud = new NumericUpDown
            {
                Location = new Point(130, y),
                Size = new Size(280, 25),
                Minimum = min,
                Maximum = max,
                Value = value,
                DecimalPlaces = 0
            };
            this.Controls.Add(nud);
            return nud;
        }

        private void LoadData()
        {
            if (_phong == null) return;
            txtMaPhong.Text = _phong.MaPhong;
            cboBuilding.SelectedValue = _phong.BuildingId;
            if (_phong.LoaiPhongId.HasValue)
                cboLoaiPhong.SelectedValue = _phong.LoaiPhongId;
            nudTang.Value = _phong.Tang;
            nudDienTich.Value = _phong.DienTich ?? 20;
            nudGiaThue.Value = _phong.GiaThue;
            nudSoNguoi.Value = _phong.SoNguoiToiDa;
            cboTrangThai.Text = _phong.TrangThai;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var phong = _phong ?? new PhongTro();
            phong.MaPhong = txtMaPhong.Text.Trim();
            phong.BuildingId = (int)cboBuilding.SelectedValue!;
            phong.LoaiPhongId = (int?)cboLoaiPhong.SelectedValue;
            phong.Tang = (int)nudTang.Value;
            phong.DienTich = nudDienTich.Value;
            phong.GiaThue = nudGiaThue.Value;
            phong.SoNguoiToiDa = (int)nudSoNguoi.Value;
            phong.TrangThai = cboTrangThai.Text;

            try
            {
                if (_phong == null)
                {
                    var (success, message, _) = await _service.CreateAsync(phong);
                    if (success) { UIHelper.ShowSuccess(message); this.DialogResult = DialogResult.OK; }
                    else UIHelper.ShowError(message);
                }
                else
                {
                    var (success, message) = await _service.UpdateAsync(phong);
                    if (success) { UIHelper.ShowSuccess(message); this.DialogResult = DialogResult.OK; }
                    else UIHelper.ShowError(message);
                }
            }
            catch (Exception ex) { UIHelper.ShowError($"Lỗi: {ex.Message}"); }
        }
    }
}
