using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucDichVu : UserControl
    {
        private readonly DichVuService _service = new();
        private DataGridView dgv = null!;
        private DichVu? _selected;

        public ucDichVu()
        {
            InitializeComponent();
            CreateLayout();
            LoadData();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ThemeManager.Surface, Padding = new Padding(15, 10, 15, 10) };
            var btnAdd = new Button { Text = "➕ Thêm", Size = new Size(90, 35), Location = new Point(15, 12), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAdd.Click += BtnAdd_Click;
            var btnEdit = new Button { Text = "✏ Sửa", Size = new Size(80, 35), Location = new Point(115, 12), BackColor = Color.FromArgb(234, 179, 8), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnEdit.Click += BtnEdit_Click;
            var btnDelete = new Button { Text = "🗑 Xóa", Size = new Size(80, 35), Location = new Point(205, 12), BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelete.Click += BtnDelete_Click;
            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "MaDichVu", "Mã DV", "MaDichVu", 80);
            UIHelper.AddColumn(dgv, "TenDichVu", "Tên dịch vụ", "TenDichVu", 200);
            UIHelper.AddColumn(dgv, "DonGia", "Đơn giá", "DonGia", 100);
            UIHelper.AddColumn(dgv, "DonViTinh", "Đơn vị", "DonViTinh", 80);
            UIHelper.AddColumn(dgv, "LoaiDichVu", "Loại", "LoaiDichVu", 100);
            UIHelper.AddColumn(dgv, "MoTa", "Mô tả", "MoTa", 200);
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is DichVu d) _selected = d; };
            dgv.CellFormatting += (s, e) => { if (dgv.Columns[e.ColumnIndex].Name == "DonGia" && e.Value != null) e.Value = string.Format("{0:N0}", e.Value); };

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private async void LoadData()
        {
            dgv.DataSource = (await _service.GetAllAsync()).ToList();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var frm = new frmDichVuEdit();
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn dịch vụ!"); return; }
            using var frm = new frmDichVuEdit(_selected);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn dịch vụ!"); return; }
            if (!UIHelper.Confirm($"Xóa dịch vụ '{_selected.TenDichVu}'?")) return;
            var (ok, msg) = await _service.DeleteAsync(_selected.DichVuId);
            if (ok) { UIHelper.ShowSuccess(msg); LoadData(); }
            else UIHelper.ShowError(msg);
        }

        private void InitializeComponent() { this.Name = "ucDichVu"; this.Size = new Size(1100, 700); }
    }

    public class frmDichVuEdit : Form
    {
        private readonly DichVuService _service = new();
        private readonly DichVu? _dichVu;
        private TextBox txtMa = null!, txtTen = null!, txtDonVi = null!, txtMoTa = null!;
        private NumericUpDown nudDonGia = null!;
        private ComboBox cboLoai = null!;

        public frmDichVuEdit(DichVu? dichVu = null)
        {
            _dichVu = dichVu;
            this.Text = _dichVu == null ? "Thêm dịch vụ" : "Sửa dịch vụ";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
            CreateControls();
            if (_dichVu != null) LoadData();
        }

        private void CreateControls()
        {
            int y = 20;
            AddLabel("Mã DV:", y); txtMa = AddTextBox(y); y += 40;
            AddLabel("Tên dịch vụ:", y); txtTen = AddTextBox(y); y += 40;
            AddLabel("Đơn giá:", y);
            nudDonGia = new NumericUpDown { Location = new Point(120, y), Size = new Size(230, 25), Maximum = 100000000, ThousandsSeparator = true };
            this.Controls.Add(nudDonGia); y += 40;
            AddLabel("Đơn vị:", y); txtDonVi = AddTextBox(y); y += 40;
            AddLabel("Loại DV:", y);
            cboLoai = new ComboBox { Location = new Point(120, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboLoai.Items.AddRange(new object[] { "CoDinh", "TheoChiSo" });
            cboLoai.SelectedIndex = 0;
            this.Controls.Add(cboLoai); y += 40;
            AddLabel("Mô tả:", y); txtMoTa = AddTextBox(y); y += 50;

            var btnSave = new Button { Text = "💾 Lưu", Location = new Point(120, y), Size = new Size(100, 35), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(230, y), Size = new Size(80, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddLabel(string text, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 5), AutoSize = true });
        private TextBox AddTextBox(int y) { var t = new TextBox { Location = new Point(120, y), Size = new Size(230, 25) }; this.Controls.Add(t); return t; }

        private void LoadData()
        {
            if (_dichVu == null) return;
            txtMa.Text = _dichVu.MaDichVu; txtMa.Enabled = false;
            txtTen.Text = _dichVu.TenDichVu;
            nudDonGia.Value = _dichVu.DonGia;
            txtDonVi.Text = _dichVu.DonViTinh;
            cboLoai.Text = _dichVu.LoaiDichVu;
            txtMoTa.Text = _dichVu.MoTa;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var dv = _dichVu ?? new DichVu();
            dv.MaDichVu = txtMa.Text.Trim();
            dv.TenDichVu = txtTen.Text.Trim();
            dv.DonGia = nudDonGia.Value;
            dv.DonViTinh = txtDonVi.Text.Trim();
            dv.LoaiDichVu = cboLoai.Text;
            dv.MoTa = txtMoTa.Text.Trim();

            if (_dichVu == null)
            {
                var (ok, msg, _) = await _service.CreateAsync(dv);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            }
            else
            {
                var (ok, msg) = await _service.UpdateAsync(dv);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            }
        }
    }
}
