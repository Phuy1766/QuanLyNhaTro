using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucBuilding : UserControl
    {
        private readonly BuildingService _service = new();
        private DataGridView dgv = null!;
        private Building? _selectedBuilding;

        public ucBuilding()
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

            var btnAdd = CreateButton("➕ Thêm mới", ThemeManager.Primary);
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = CreateButton("✏ Sửa", Color.FromArgb(234, 179, 8));
            btnEdit.Location = new Point(130, 10);
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = CreateButton("🗑 Xóa", Color.FromArgb(239, 68, 68));
            btnDelete.Location = new Point(230, 10);
            btnDelete.Click += BtnDelete_Click;

            var btnRefresh = CreateButton("🔄 Làm mới", ThemeManager.Secondary);
            btnRefresh.Location = new Point(330, 10);
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
                if (dgv.CurrentRow?.DataBoundItem is Building b)
                    _selectedBuilding = b;
            };

            pnlGrid.Controls.Add(dgv);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                Size = new Size(110, 35),
                Location = new Point(15, 10),
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
            UIHelper.AddColumn(dgv, "BuildingCode", "Mã", "BuildingCode", 80);
            UIHelper.AddColumn(dgv, "BuildingName", "Tên tòa nhà", "BuildingName", 200);
            UIHelper.AddColumn(dgv, "Address", "Địa chỉ", "Address", 300);
            UIHelper.AddColumn(dgv, "TotalFloors", "Số tầng", "TotalFloors", 80);
            UIHelper.AddColumn(dgv, "TotalRooms", "Tổng phòng", "TotalRooms", 100);
            UIHelper.AddColumn(dgv, "AvailableRooms", "Phòng trống", "AvailableRooms", 100);
        }

        private async void LoadData()
        {
            try
            {
                var data = await _service.GetAllAsync();
                dgv.DataSource = data.ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var frm = new frmBuildingEdit();
            if (frm.ShowDialog() == DialogResult.OK)
                LoadData();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selectedBuilding == null)
            {
                UIHelper.ShowWarning("Vui lòng chọn tòa nhà cần sửa!");
                return;
            }

            using var frm = new frmBuildingEdit(_selectedBuilding);
            if (frm.ShowDialog() == DialogResult.OK)
                LoadData();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedBuilding == null)
            {
                UIHelper.ShowWarning("Vui lòng chọn tòa nhà cần xóa!");
                return;
            }

            if (!UIHelper.Confirm($"Bạn có chắc muốn xóa tòa nhà '{_selectedBuilding.BuildingName}'?"))
                return;

            var (success, message) = await _service.DeleteAsync(_selectedBuilding.BuildingId);
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "ucBuilding";
            this.Size = new Size(1100, 700);
            this.ResumeLayout(false);
        }
    }

    // Form Add/Edit Building
    public class frmBuildingEdit : Form
    {
        private readonly BuildingService _service = new();
        private readonly Building? _building;

        private TextBox txtCode = null!;
        private TextBox txtName = null!;
        private TextBox txtAddress = null!;
        private NumericUpDown nudFloors = null!;
        private TextBox txtDescription = null!;

        public frmBuildingEdit(Building? building = null)
        {
            _building = building;
            SetupForm();
            CreateControls();
            if (_building != null)
                LoadData();
        }

        private void SetupForm()
        {
            this.Text = _building == null ? "Thêm tòa nhà" : "Sửa tòa nhà";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
        }

        private void CreateControls()
        {
            int y = 20;

            AddLabel("Mã tòa nhà:", y);
            txtCode = AddTextBox(y); y += 50;

            AddLabel("Tên tòa nhà:", y);
            txtName = AddTextBox(y); y += 50;

            AddLabel("Địa chỉ:", y);
            txtAddress = AddTextBox(y); y += 50;

            AddLabel("Số tầng:", y);
            nudFloors = new NumericUpDown
            {
                Location = new Point(130, y),
                Size = new Size(280, 30),
                Minimum = 1,
                Maximum = 100,
                Value = 1
            };
            this.Controls.Add(nudFloors);
            y += 50;

            AddLabel("Mô tả:", y);
            txtDescription = AddTextBox(y);
            txtDescription.Multiline = true;
            txtDescription.Height = 60;
            y += 80;

            // Buttons
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
            var lbl = new Label
            {
                Text = text,
                Location = new Point(20, y + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(lbl);
        }

        private TextBox AddTextBox(int y)
        {
            var txt = new TextBox
            {
                Location = new Point(130, y),
                Size = new Size(280, 30),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txt);
            return txt;
        }

        private void LoadData()
        {
            if (_building == null) return;
            txtCode.Text = _building.BuildingCode;
            txtCode.Enabled = false;
            txtName.Text = _building.BuildingName;
            txtAddress.Text = _building.Address;
            nudFloors.Value = _building.TotalFloors;
            txtDescription.Text = _building.Description;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var building = _building ?? new Building();
            building.BuildingCode = txtCode.Text.Trim();
            building.BuildingName = txtName.Text.Trim();
            building.Address = txtAddress.Text.Trim();
            building.TotalFloors = (int)nudFloors.Value;
            building.Description = txtDescription.Text.Trim();

            try
            {
                if (_building == null)
                {
                    var (success, message, _) = await _service.CreateAsync(building);
                    if (success)
                    {
                        UIHelper.ShowSuccess(message);
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                        UIHelper.ShowError(message);
                }
                else
                {
                    var (success, message) = await _service.UpdateAsync(building);
                    if (success)
                    {
                        UIHelper.ShowSuccess(message);
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                        UIHelper.ShowError(message);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }
    }
}
