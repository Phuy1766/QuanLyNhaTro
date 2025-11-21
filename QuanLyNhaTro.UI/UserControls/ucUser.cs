using QuanLyNhaTro.BLL.Services;
using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucUser : UserControl
    {
        private readonly UserService _service = new();
        private readonly AuthService _authService = new();
        private DataGridView dgv = null!;
        private User? _selected;

        public ucUser()
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
            var btnToggle = new Button { Text = "🔒 Khóa/Mở", Size = new Size(100, 35), Location = new Point(205, 12), BackColor = Color.FromArgb(168, 85, 247), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnToggle.Click += BtnToggle_Click;
            var btnReset = new Button { Text = "🔑 Reset MK", Size = new Size(100, 35), Location = new Point(315, 12), BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReset.Click += BtnReset_Click;

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnToggle, btnReset });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.Surface, Padding = new Padding(15) };
            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.Surface, BorderStyle = BorderStyle.None };
            UIHelper.StyleDataGridView(dgv);
            dgv.Columns.Clear();
            UIHelper.AddColumn(dgv, "Username", "Username", "Username", 120);
            UIHelper.AddColumn(dgv, "FullName", "Họ tên", "FullName", 180);
            UIHelper.AddColumn(dgv, "Email", "Email", "Email", 180);
            UIHelper.AddColumn(dgv, "Phone", "SĐT", "Phone", 100);
            UIHelper.AddColumn(dgv, "RoleName", "Vai trò", "RoleName", 100);
            UIHelper.AddColumn(dgv, "IsActive", "Trạng thái", "IsActive", 100);
            UIHelper.AddColumn(dgv, "LastLogin", "Đăng nhập cuối", "LastLogin", 130);
            dgv.SelectionChanged += (s, e) => { if (dgv.CurrentRow?.DataBoundItem is User u) _selected = u; };
            dgv.CellFormatting += Dgv_CellFormatting;

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "IsActive" && e.Value != null)
            {
                bool active = (bool)e.Value;
                e.Value = active ? "Hoạt động" : "Đã khóa";
                e.CellStyle!.ForeColor = active ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            }
            if (dgv.Columns[e.ColumnIndex].Name == "LastLogin" && e.Value != null)
                e.Value = ((DateTime)e.Value).ToString("dd/MM/yyyy HH:mm");
        }

        private async void LoadData() { dgv.DataSource = (await _service.GetAllAsync()).ToList(); }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            var roles = await _service.GetRolesAsync();
            using var frm = new frmUserEdit(roles);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn tài khoản!"); return; }
            var roles = await _service.GetRolesAsync();
            using var frm = new frmUserEdit(roles, _selected);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private async void BtnToggle_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn tài khoản!"); return; }
            var action = _selected.IsActive ? "khóa" : "mở khóa";
            if (!UIHelper.Confirm($"Bạn có chắc muốn {action} tài khoản '{_selected.Username}'?")) return;
            var (ok, msg) = await _service.ToggleActiveAsync(_selected.UserId);
            if (ok) { UIHelper.ShowSuccess(msg); LoadData(); }
            else UIHelper.ShowError(msg);
        }

        private async void BtnReset_Click(object? sender, EventArgs e)
        {
            if (_selected == null) { UIHelper.ShowWarning("Vui lòng chọn tài khoản!"); return; }
            if (!UIHelper.Confirm($"Reset mật khẩu cho '{_selected.Username}'?")) return;
            var (ok, msg, newPass) = await _authService.ResetPasswordAsync(_selected.UserId);
            if (ok) MessageBox.Show($"Mật khẩu mới: {newPass}", "Reset thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else UIHelper.ShowError(msg);
        }

        private void InitializeComponent() { this.Name = "ucUser"; this.Size = new Size(1100, 700); }
    }

    public class frmUserEdit : Form
    {
        private readonly UserService _service = new();
        private readonly User? _user;
        private TextBox txtUsername = null!, txtFullName = null!, txtEmail = null!, txtPhone = null!, txtPassword = null!;
        private ComboBox cboRole = null!;
        private CheckBox chkActive = null!;

        public frmUserEdit(IEnumerable<Role> roles, User? user = null)
        {
            _user = user;
            this.Text = _user == null ? "Thêm tài khoản" : "Sửa tài khoản";
            this.Size = new Size(400, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
            CreateControls(roles);
            if (_user != null) LoadData();
        }

        private void CreateControls(IEnumerable<Role> roles)
        {
            int y = 20;
            AddLabel("Username:", y); txtUsername = AddTextBox(y); y += 40;
            AddLabel("Mật khẩu:", y); txtPassword = AddTextBox(y); txtPassword.PasswordChar = '*';
            if (_user != null) { txtPassword.PlaceholderText = "(để trống nếu không đổi)"; }
            y += 40;
            AddLabel("Họ tên:", y); txtFullName = AddTextBox(y); y += 40;
            AddLabel("Email:", y); txtEmail = AddTextBox(y); y += 40;
            AddLabel("SĐT:", y); txtPhone = AddTextBox(y); y += 40;
            AddLabel("Vai trò:", y);
            cboRole = new ComboBox { Location = new Point(100, y), Size = new Size(260, 25), DropDownStyle = ComboBoxStyle.DropDownList, DataSource = roles.ToList(), DisplayMember = "RoleName", ValueMember = "RoleId" };
            this.Controls.Add(cboRole); y += 40;
            chkActive = new CheckBox { Text = "Hoạt động", Location = new Point(100, y), Checked = true };
            this.Controls.Add(chkActive); y += 45;

            var btnSave = new Button { Text = "💾 Lưu", Location = new Point(100, y), Size = new Size(100, 35), BackColor = ThemeManager.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            var btnCancel = new Button { Text = "Hủy", Location = new Point(210, y), Size = new Size(80, 35), BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        private void AddLabel(string text, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 5), AutoSize = true });
        private TextBox AddTextBox(int y) { var t = new TextBox { Location = new Point(100, y), Size = new Size(260, 25) }; this.Controls.Add(t); return t; }

        private void LoadData()
        {
            if (_user == null) return;
            txtUsername.Text = _user.Username; txtUsername.Enabled = false;
            txtFullName.Text = _user.FullName;
            txtEmail.Text = _user.Email;
            txtPhone.Text = _user.Phone;
            cboRole.SelectedValue = _user.RoleId;
            chkActive.Checked = _user.IsActive;
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var user = _user ?? new User();
            user.Username = txtUsername.Text.Trim();
            user.FullName = txtFullName.Text.Trim();
            user.Email = txtEmail.Text.Trim();
            user.Phone = txtPhone.Text.Trim();
            user.RoleId = (int)cboRole.SelectedValue!;
            user.IsActive = chkActive.Checked;

            if (_user == null)
            {
                if (string.IsNullOrEmpty(txtPassword.Text)) { UIHelper.ShowWarning("Vui lòng nhập mật khẩu!"); return; }
                var (ok, msg, _) = await _service.CreateAsync(user, txtPassword.Text);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            }
            else
            {
                var (ok, msg) = await _service.UpdateAsync(user);
                if (ok) { UIHelper.ShowSuccess(msg); this.DialogResult = DialogResult.OK; }
                else UIHelper.ShowError(msg);
            }
        }
    }
}
