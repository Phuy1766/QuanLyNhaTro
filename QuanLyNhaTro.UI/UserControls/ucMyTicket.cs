using QuanLyNhaTro.DAL.Models;
using QuanLyNhaTro.DAL.Repositories;
using QuanLyNhaTro.UI.Helpers;
using QuanLyNhaTro.UI.Themes;

namespace QuanLyNhaTro.UI.UserControls
{
    public partial class ucMyTicket : UserControl
    {
        private readonly BaoTriRepository _baoTriRepo = new();
        private readonly HopDongRepository _hopDongRepo = new();
        private readonly int _tenantUserId;
        private int _khachId;
        private string _maPhong = "";

        private DataGridView dgv = null!;
        private Panel pnlForm = null!;
        private ComboBox cboLoai = null!;
        private TextBox txtTieuDe = null!, txtMoTa = null!;

        public ucMyTicket(int tenantUserId)
        {
            _tenantUserId = tenantUserId;
            InitializeComponent();
            CreateLayout();
            LoadDataAsync();
        }

        private void CreateLayout()
        {
            this.BackColor = ThemeManager.Background;

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeManager.Surface,
                Padding = new Padding(20)
            };

            var lblTitle = new Label
            {
                Text = "Yêu cầu bảo trì",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            pnlHeader.Controls.Add(lblTitle);

            var btnNew = new Button
            {
                Text = "+ Gửi yêu cầu mới",
                Size = new Size(150, 35),
                Location = new Point(900, 12),
                BackColor = ThemeManager.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnNew.Click += BtnNew_Click;
            pnlHeader.Controls.Add(btnNew);
            this.Controls.Add(pnlHeader);

            // Form Panel (hidden initially)
            pnlForm = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(1050, 200),
                BackColor = ThemeManager.Surface,
                Visible = false,
                Padding = new Padding(20)
            };
            CreateFormControls();
            this.Controls.Add(pnlForm);

            // Grid Panel
            var pnlGrid = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(1050, 550),
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

            UIHelper.AddColumn(dgv, "TicketId", "Mã", "TicketId", 60);
            UIHelper.AddColumn(dgv, "LoaiSuCo", "Loại", "LoaiSuCo", 100);
            UIHelper.AddColumn(dgv, "TieuDe", "Tiêu đề", "TieuDe", 200);
            UIHelper.AddColumn(dgv, "MoTa", "Mô tả", "MoTa", 200);
            UIHelper.AddColumn(dgv, "TrangThai", "Trạng thái", "TrangThai", 100);
            UIHelper.AddColumn(dgv, "NgayTao", "Ngày gửi", "NgayTao", 100);
            UIHelper.AddColumn(dgv, "NguoiXuLy", "Người xử lý", "NguoiXuLy", 120);
            UIHelper.AddColumn(dgv, "GhiChu", "Ghi chú", "GhiChu", 150);

            pnlGrid.Controls.Add(dgv);
            this.Controls.Add(pnlGrid);
        }

        private void CreateFormControls()
        {
            var lblFormTitle = new Label
            {
                Text = "Gửi yêu cầu bảo trì mới",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.Primary,
                Location = new Point(15, 10),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblFormTitle);

            // Loại sự cố
            pnlForm.Controls.Add(new Label { Text = "Loại sự cố:", Location = new Point(15, 50), AutoSize = true });
            cboLoai = new ComboBox
            {
                Location = new Point(120, 47),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLoai.Items.AddRange(new object[] { "Điện", "Nước", "Thiết bị", "Cửa/Khóa", "Điều hòa", "Khác" });
            cboLoai.SelectedIndex = 0;
            pnlForm.Controls.Add(cboLoai);

            // Tiêu đề
            pnlForm.Controls.Add(new Label { Text = "Tiêu đề:", Location = new Point(350, 50), AutoSize = true });
            txtTieuDe = new TextBox { Location = new Point(420, 47), Size = new Size(300, 25) };
            pnlForm.Controls.Add(txtTieuDe);

            // Mô tả
            pnlForm.Controls.Add(new Label { Text = "Mô tả chi tiết:", Location = new Point(15, 90), AutoSize = true });
            txtMoTa = new TextBox
            {
                Location = new Point(120, 87),
                Size = new Size(600, 60),
                Multiline = true
            };
            pnlForm.Controls.Add(txtMoTa);

            // Buttons
            var btnSubmit = new Button
            {
                Text = "Gửi yêu cầu",
                Location = new Point(750, 87),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSubmit.Click += BtnSubmit_Click;
            pnlForm.Controls.Add(btnSubmit);

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(880, 87),
                Size = new Size(80, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => { pnlForm.Visible = false; AdjustGridPosition(false); };
            pnlForm.Controls.Add(btnCancel);
        }

        private void AdjustGridPosition(bool formVisible)
        {
            var pnlGrid = this.Controls.OfType<Panel>().LastOrDefault();
            if (pnlGrid != null && pnlGrid != pnlForm)
            {
                pnlGrid.Location = new Point(20, formVisible ? 280 : 80);
                pnlGrid.Size = new Size(1050, formVisible ? 350 : 550);
            }
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            txtTieuDe.Clear();
            txtMoTa.Clear();
            cboLoai.SelectedIndex = 0;
            pnlForm.Visible = true;
            AdjustGridPosition(true);
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTieuDe.Text))
            {
                UIHelper.ShowWarning("Vui lòng nhập tiêu đề!");
                return;
            }

            if (_khachId == 0)
            {
                UIHelper.ShowError("Không tìm thấy thông tin khách thuê!");
                return;
            }

            try
            {
                var ticket = new BaoTriTicket
                {
                    MaPhong = _maPhong,
                    KhachId = _khachId,
                    LoaiSuCo = cboLoai.SelectedItem?.ToString() ?? "Khác",
                    TieuDe = txtTieuDe.Text.Trim(),
                    MoTa = txtMoTa.Text.Trim(),
                    TrangThai = "Mới",
                    DoUuTien = "Trung bình"
                };

                await _baoTriRepo.AddAsync(ticket);
                UIHelper.ShowSuccess("Gửi yêu cầu bảo trì thành công!");

                pnlForm.Visible = false;
                AdjustGridPosition(false);
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private async void LoadDataAsync()
        {
            try
            {
                var contract = await _hopDongRepo.GetActiveByUserIdAsync(_tenantUserId);
                if (contract == null)
                {
                    UIHelper.ShowWarning("Bạn chưa có hợp đồng thuê phòng");
                    return;
                }

                _khachId = contract.KhachId;
                _maPhong = contract.MaPhong;

                var tickets = await _baoTriRepo.GetByTenantAsync(_khachId);
                dgv.DataSource = tickets.ToList();

                // Format
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Name == "NgayTao") col.DefaultCellStyle.Format = "dd/MM/yyyy";
                }

                // Color status
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var status = row.Cells["TrangThai"].Value?.ToString();
                    if (status == "Hoàn thành")
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                    else if (status == "Đang xử lý")
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(59, 130, 246);
                    else
                        row.Cells["TrangThai"].Style.ForeColor = Color.FromArgb(245, 158, 11);
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            this.Name = "ucMyTicket";
            this.Size = new Size(1100, 700);
        }
    }
}
