using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace BT66;
static class Program {
 [STAThread] static void Main(){ Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); try { Application.Run(new frmBankAtmMain()); } catch (Exception ex) { System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BT66-error.txt"), ex.ToString()); MessageBox.Show(ex.ToString(), "Lỗi khởi động"); } }
}
public class frmBankAtmMain : Form {
 readonly ToolStripProgressBar progress = new(){Minimum=0,Maximum=100,Width=180};
 readonly ToolStripStatusLabel state = new("Core Banking: Kết nối mô phỏng");
 public frmBankAtmMain(){
 Text="Hệ thống điều hành ATM ngân hàng"; IsMdiContainer=true; WindowState=FormWindowState.Maximized;
 var menu=new MenuStrip(); var system=new ToolStripMenuItem("Hệ thống"); var atm=new ToolStripMenuItem("Quản lý ATM"); var windows=new ToolStripMenuItem("Cửa sổ");
 system.DropDownItems.Add("Đăng xuất",null,(_,_)=>Lock(true)); system.DropDownItems.Add("Khóa màn hình",null,(_,_)=>Lock(false)); system.DropDownItems.Add("Thoát",null,(_,_)=>Close());
 atm.DropDownItems.Add("Danh sách Trạm ATM & Tồn quỹ",null,(_,_)=>Open<frmAtmStations>()); atm.DropDownItems.Add("Đối soát Giao dịch Cuối ngày",null,(_,_)=>Open<frmAtmReconciliation>());
 windows.DropDownItems.Add("Xếp tầng",null,(_,_)=>LayoutMdi(MdiLayout.Cascade)); windows.DropDownItems.Add("Xếp ngang",null,(_,_)=>LayoutMdi(MdiLayout.TileHorizontal)); windows.DropDownItems.Add("Xếp dọc",null,(_,_)=>LayoutMdi(MdiLayout.TileVertical)); windows.DropDownItems.Add("Đóng tất cả",null,(_,_)=>{foreach(var f in MdiChildren) f.Close();});
 menu.Items.AddRange(new ToolStripItem[]{system,atm,windows}); MainMenuStrip=menu; Controls.Add(menu);
 var status=new StatusStrip(); status.Items.Add(new ToolStripStatusLabel("Giao dịch viên trực: Nhân viên 01")); status.Items.Add(state); status.Items.Add(new ToolStripStatusLabel(){Spring=true}); status.Items.Add(progress); Controls.Add(status);
 }
 void Open<T>() where T:Form,new(){foreach(var f in MdiChildren) if(f is T){if(f.WindowState==FormWindowState.Minimized)f.WindowState=FormWindowState.Normal; f.Activate();return;} new T(){MdiParent=this}.Show();}
 void Lock(bool logout){if(logout)foreach(var f in MdiChildren)f.Close(); using var dialog=new Form(){Text=logout?"Đăng nhập lại (mô phỏng)":"Màn hình đã khóa (mô phỏng)",StartPosition=FormStartPosition.CenterParent,Size=new Size(360,150),ControlBox=false}; var b=new Button(){Text="Tiếp tục phiên mô phỏng",Dock=DockStyle.Fill,DialogResult=DialogResult.OK}; dialog.Controls.Add(b);dialog.ShowDialog(this);}
 public void Report(int value,string message){progress.Value=value;state.Text="Core Banking: Kết nối mô phỏng | "+message;}
}
public class Station {public string Code{get;set;}="";public string Address{get;set;}="";public decimal Cash{get;set;} public string Status{get;set;}="";public string Photo{get;set;}="";}
public class frmAtmStations:Form {
 readonly BindingList<Station> data=new(); readonly DataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,ReadOnly=true,AllowUserToAddRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly TextBox code=new(), address=new(), photo=new(){ReadOnly=true}; readonly NumericUpDown cash=new(){Maximum=1000000000000M,ThousandsSeparator=true}; readonly ComboBox status=new(){DropDownStyle=ComboBoxStyle.DropDownList}; readonly PictureBox preview=new(){SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Right,Width=180};
 public frmAtmStations(){Text="Quản lý trạm ATM và tồn quỹ";Size=new Size(950,520); status.Items.AddRange(new object[]{"Hoạt động","Bảo trì","Ngừng hoạt động"});status.SelectedIndex=0;
 var panel=new TableLayoutPanel(){Dock=DockStyle.Top,Height=160,ColumnCount=2,RowCount=5}; panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
 var fields=new Control[]{code,address,cash,status,photo};var labels=new[]{"Mã trạm","Địa chỉ","Tồn quỹ (VNĐ)","Trạng thái","Ảnh hiện trường"};for(int i=0;i<5;i++){panel.RowStyles.Add(new RowStyle(SizeType.Percent,20));panel.Controls.Add(new Label(){Text=labels[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,i);fields[i].Dock=DockStyle.Fill;panel.Controls.Add(fields[i],1,i);}
 var buttons=new FlowLayoutPanel(){Dock=DockStyle.Top,Height=42}; AddButton(buttons,"Chọn ảnh",Choose);AddButton(buttons,"Thêm",()=>Save(false));AddButton(buttons,"Cập nhật",()=>Save(true));AddButton(buttons,"Xóa",()=>{if(grid.CurrentRow?.DataBoundItem is Station s && MessageBox.Show("Xóa trạm?","Xác nhận",MessageBoxButtons.YesNo)==DialogResult.Yes)data.Remove(s);});AddButton(buttons,"Làm mới",()=>{grid.ClearSelection();code.Clear();address.Clear();cash.Value=0;status.SelectedIndex=0;photo.Clear();ShowPhoto();});
 var props=new[]{"Code","Address","Cash","Status","Photo"};for(int i=0;i<5;i++)grid.Columns.Add(new DataGridViewTextBoxColumn(){DataPropertyName=props[i],HeaderText=labels[i]});grid.Columns[2].DefaultCellStyle.Format="N0";grid.DataSource=data;grid.SelectionChanged+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is Station s){code.Text=s.Code;address.Text=s.Address;cash.Value=s.Cash;status.SelectedItem=s.Status;photo.Text=s.Photo;ShowPhoto();}};
 Controls.Add(grid);Controls.Add(preview);Controls.Add(buttons);Controls.Add(panel);data.Add(new Station(){Code="ATM01",Address="Hà Nội",Cash=500000000,Status="Hoạt động"});data.Add(new Station(){Code="ATM02",Address="Cầu Giấy",Cash=200000000,Status="Bảo trì"}); }
 void AddButton(FlowLayoutPanel p,string t,Action a){var b=new Button(){Text=t,AutoSize=true};b.Click+=(_,_)=>a();p.Controls.Add(b);}
 void Choose(){using var d=new OpenFileDialog(){Filter="Ảnh|*.jpg;*.jpeg;*.png;*.bmp"};if(d.ShowDialog()==DialogResult.OK){photo.Text=d.FileName;ShowPhoto();}}
 void ShowPhoto(){preview.Image?.Dispose();preview.Image=null;try{if(System.IO.File.Exists(photo.Text)){using var img=Image.FromFile(photo.Text);preview.Image=new Bitmap(img);}}catch{}}
 void Save(bool update){var old=update?grid.CurrentRow?.DataBoundItem as Station:null;if(update&&old==null)return;if(string.IsNullOrWhiteSpace(code.Text)||string.IsNullOrWhiteSpace(address.Text)){MessageBox.Show("Nhập mã và địa chỉ.");return;}foreach(var s in data)if(s!=old&&string.Equals(s.Code,code.Text.Trim(),StringComparison.OrdinalIgnoreCase)){MessageBox.Show("Mã trạm đã tồn tại.");return;}var item=new Station(){Code=code.Text.Trim(),Address=address.Text.Trim(),Cash=cash.Value,Status=status.Text,Photo=photo.Text};if(old==null)data.Add(item);else data[data.IndexOf(old)]=item;}
 protected override void OnFormClosed(FormClosedEventArgs e){preview.Image?.Dispose();base.OnFormClosed(e);}
}
public class frmAtmReconciliation:Form {
 CancellationTokenSource cts; bool closing; readonly Button run=new(){Text="Chạy Đối Soát Tự Động",AutoSize=true};readonly Button stop=new(){Text="Dừng Đối Soát",AutoSize=true,Enabled=false};readonly Label info=new(){Text="Sẵn sàng",Dock=DockStyle.Top,Height=40};readonly ListBox log=new(){Dock=DockStyle.Fill};
 public frmAtmReconciliation(){Text="Đối soát giao dịch cuối ngày";Size=new Size(700,420);var p=new FlowLayoutPanel(){Dock=DockStyle.Top,Height=45};p.Controls.AddRange(new Control[]{run,stop});Controls.Add(log);Controls.Add(info);Controls.Add(p);run.Click+=async (_,_)=>await Execute();stop.Click+=(_,_)=>cts?.Cancel();FormClosing+=(_,_)=>{closing=true;cts?.Cancel();};}
 async Task Execute(){if(cts!=null)return;var source=new CancellationTokenSource();cts=source;var token=source.Token;run.Enabled=false;stop.Enabled=true;log.Items.Clear();var main=MdiParent as frmBankAtmMain;var progress=new Progress<int>(v=>{if(closing||cts!=source)return;info.Text=$"Đang đối soát: {v}%";main?.Report(v,"Đang đối soát");log.Items.Add($"Đã xử lý {v*2000:N0} giao dịch mô phỏng");});try{await Task.Run(async ()=>{for(int i=1;i<=20;i++){token.ThrowIfCancellationRequested();await Task.Delay(250,token);long sum=0;for(int j=0;j<10000;j++){token.ThrowIfCancellationRequested();sum+=j;}((IProgress<int>)progress).Report(i*5);}},token);if(!closing){info.Text="Hoàn tất 200.000 giao dịch mô phỏng";main?.Report(100,"Hoàn tất");}}catch(OperationCanceledException){if(!closing){info.Text="Đã dừng đối soát";main?.Report(0,"Đã dừng");}}finally{cts=null;source.Dispose();if(!closing){run.Enabled=true;stop.Enabled=false;}}}
 protected override void OnFormClosed(FormClosedEventArgs e){if(cts!=null && MdiParent is frmBankAtmMain m && !m.IsDisposed)m.Report(0,"Đã dừng");base.OnFormClosed(e);}
}
