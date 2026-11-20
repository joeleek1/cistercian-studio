using System;
using System.Drawing;
using System.Windows.Forms;

namespace CistercianStudio
{
    public sealed class SettingsWindow : Form
    {
        private readonly ClockWindow clock;
        private readonly ClockSettings settings;
        private readonly IWindowsIntegration integration;
        private CheckBox startupCheck;
        private Label windowsStatus;
        private bool updatingStartup;
        private readonly Panel body = new Panel();
        private readonly NumericUpDown widthInput, heightInput;
        private readonly ComboBox dateStyle, dateFormat;
        private readonly Label dateExample;
        private bool syncing;
        private int rowY = 16;
        private static readonly Color Green = Color.FromArgb(133,236,166), Canvas = Color.FromArgb(12,22,16), Field = Color.FromArgb(25,41,31);
        public SettingsWindow(ClockWindow owner, ClockSettings value, IWindowsIntegration windows = null)
        {
            clock=owner; settings=value; integration=windows ?? new WindowsIntegration();
            Text="Cistercian Clock - Settings"; Font=new Font("Segoe UI",9f); BackColor=Canvas; ForeColor=Green;
            ClientSize=new Size(470,670); MinimumSize=new Size(460,480); FormBorderStyle=FormBorderStyle.SizableToolWindow;
            StartPosition=FormStartPosition.CenterParent; ShowInTaskbar=false; TopMost=owner.TopMost;
            var close = new Button { Text="Done", Dock=DockStyle.Bottom, Height=40, BackColor=Field, ForeColor=Green, FlatStyle=FlatStyle.Flat };
            close.Click+=delegate{Close();}; Controls.Add(close); AcceptButton=close; CancelButton=close;
            body.Dock=DockStyle.Fill; body.AutoScroll=true; body.Padding=new Padding(0,0,0,12); Controls.Add(body); body.BringToFront();
            Heading("WINDOWS");
            startupCheck = new CheckBox { Text="Start with Windows", Left=16, Top=rowY, Width=420, Height=28, ForeColor=Green, AccessibleName="Start with Windows" };
            body.Controls.Add(startupCheck); rowY+=31;
            startupCheck.CheckedChanged+=delegate { ChangeStartup(); };
            Note("Open the clock when you sign in after a reboot. Keep the app in a permanent folder.");
            var desktopButton = new Button { Text="Create desktop shortcut", Left=16, Top=rowY, Width=250, Height=30, BackColor=Field, ForeColor=Green, FlatStyle=FlatStyle.Flat, AccessibleName="Create desktop shortcut" };
            desktopButton.Click+=delegate { try { integration.CreateDesktopShortcut(); windowsStatus.Text="Desktop shortcut created."; } catch(Exception error) { windowsStatus.Text="Shortcut could not be created: "+error.Message; } };
            body.Controls.Add(desktopButton); rowY+=36;
            windowsStatus=LabelAt("",16,rowY,420,50); rowY+=53;
            RefreshStartup();
            Activated+=delegate { RefreshStartup(); };
            Heading("APPEARANCE");
            Choice("Theme",new[]{"Phosphor - green on black","Amber","Ice","Paper"},Array.IndexOf(new[]{"Phosphor","Amber","Ice","Paper"},settings.Theme),i=>{settings.Theme=new[]{"Phosphor","Amber","Ice","Paper"}[i];Changed();});
            var opacityLabel=LabelAt("Window transparency",16,rowY,420,22);rowY+=23;
            var slider=new TrackBar{Left=13,Top=rowY,Width=420,Height=34,Minimum=0,Maximum=60,TickFrequency=10,SmallChange=5,LargeChange=10,Value=100-settings.Opacity,BackColor=Canvas,AccessibleName="Window transparency"};
            Action updateOpacity=()=>opacityLabel.Text="Window transparency: "+slider.Value+"% (symbols and background)";
            updateOpacity();slider.ValueChanged+=delegate{settings.Opacity=100-slider.Value;updateOpacity();Changed();};body.Controls.Add(slider);rowY+=42;
            Toggle("Transparent background (floating symbols)",settings.TransparentBackground,v=>{settings.TransparentBackground=v;Changed();});
            Note("Transparent background: drag a symbol to move; use the lower-right grip to resize.");
            Heading("SIZE");
            widthInput=Number("Width (pixels)",clock.ClientSize.Width,240,1600);
            heightInput=Number("Height (pixels)",clock.ClientSize.Height,150,1200);
            widthInput.ValueChanged+=delegate{DimensionsChanged();};heightInput.ValueChanged+=delegate{DimensionsChanged();};
            for(int i=0;i<3;i++)
            {
                int index=i;var preset=new Button{Text=new[]{"Small","Medium","Large"}[i],Left=16+i*139,Top=rowY,Width=130,Height=29,BackColor=Field,ForeColor=Green,FlatStyle=FlatStyle.Flat};
                preset.Click+=delegate{clock.SetPreset(index);RefreshSize();};body.Controls.Add(preset);
            }
            rowY+=37;Note("You can also drag any edge or corner. Symbols keep their proportions.");
            Heading("DATE AND TIME");
            Toggle("Show date",settings.Date,v=>{settings.Date=v;Changed();DateControls();});
            dateStyle=Choice("Date display",new[]{"Cistercian symbols","Standard date text","Symbols and text"},Array.IndexOf(new[]{"Symbols","Text","Both"},settings.DateStyle),i=>{settings.DateStyle=new[]{"Symbols","Text","Both"}[i];Changed();DateControls();});
            dateFormat=Choice("Date format",new[]{"Day / month / year","Month / day / year","Year - month - day","Weekday, day month year"},Array.IndexOf(new[]{"DMY","MDY","YMD","Long"},settings.DateFormat),i=>{settings.DateFormat=new[]{"DMY","MDY","YMD","Long"}[i];Changed();DateControls();});
            dateExample=LabelAt("",16,rowY,420,30);dateExample.Font=new Font("Segoe UI",9f,FontStyle.Italic);rowY+=33;
            Choice("Time zone",new[]{"This device","London (GMT / BST)","UTC"},Array.IndexOf(new[]{"Local","London","UTC"},settings.Zone),i=>{settings.Zone=new[]{"Local","London","UTC"}[i];Changed();DateControls();});
            Toggle("Show seconds",settings.Seconds,v=>{settings.Seconds=v;Changed();});
            Toggle("Show decimal labels below symbols",settings.Labels,v=>{settings.Labels=v;Changed();});
            Heading("FINISH AND WINDOW");
            Toggle("Phosphor glow",settings.Glow,v=>{settings.Glow=v;Changed();});
            Toggle("CRT scanlines (solid dark backgrounds)",settings.Scanlines,v=>{settings.Scanlines=v;Changed();});
            Toggle("Always on top",settings.OnTop,v=>{settings.OnTop=v;Changed();TopMost=v;});
            Note("Changes save automatically. Version 1.2.0. Press Esc here to close settings.");
            body.AutoScrollMinSize=new Size(0,rowY+12);
            RefreshSize(); DateControls();
            owner.ResizeEnd+=OwnerResized;
        }
        private void RefreshStartup()
        {
            updatingStartup=true;
            try { startupCheck.Checked=integration.StartupEnabled; windowsStatus.Text=startupCheck.Checked?"Startup shortcut enabled for this Windows account.":"Automatic startup is off."; }
            catch(Exception error) { windowsStatus.Text="Could not read startup: "+error.Message; }
            finally { updatingStartup=false; }
        }
        private void ChangeStartup()
        {
            if(updatingStartup)return;
            bool previous=!startupCheck.Checked;
            try { integration.SetStartup(startupCheck.Checked); RefreshStartup(); }
            catch(Exception error)
            {
                updatingStartup=true; startupCheck.Checked=previous; updatingStartup=false;
                windowsStatus.Text="Startup could not be changed: "+error.Message;
            }
        }
        private void OwnerResized(object sender,EventArgs args){RefreshSize();}
        private void RefreshSize(){if(widthInput==null||heightInput==null)return;syncing=true;heightInput.Minimum=clock.MinimumSize.Height;widthInput.Value=Math.Max(widthInput.Minimum,Math.Min(widthInput.Maximum,clock.ClientSize.Width));heightInput.Value=Math.Max(heightInput.Minimum,Math.Min(heightInput.Maximum,clock.ClientSize.Height));syncing=false;}
        private void DimensionsChanged(){if(syncing)return;settings.Width=(int)widthInput.Value;settings.Height=(int)heightInput.Value;Changed();}
        private void Changed(){clock.ApplySettings();RefreshSize();}
        private void DateControls()
        {
            if(dateStyle==null||dateFormat==null||dateExample==null)return;
            dateStyle.Enabled=dateFormat.Enabled=settings.Date;
            var date=Numerals.TimeAt(DateTime.UtcNow,settings.Zone);
            dateExample.Text=settings.Date?DateDisplay.Text(date,settings.DateFormat)+(settings.DateFormat=="Long"?" (symbols: day / month / year)":""):"The date is hidden.";
        }
        private Label LabelAt(string text,int x,int y,int width,int height){var label=new Label{Text=text,Left=x,Top=y,Width=width,Height=height,ForeColor=Green,TextAlign=ContentAlignment.MiddleLeft};body.Controls.Add(label);return label;}
        private void Heading(string text){if(rowY>20)rowY+=12;var heading=LabelAt(text,16,rowY,420,24);heading.Font=new Font("Segoe UI",9f,FontStyle.Bold);rowY+=31;}
        private void Note(string text){var note=LabelAt(text,16,rowY,415,36);note.ForeColor=Color.FromArgb(140,170,150);note.Font=new Font("Segoe UI",8f);rowY+=39;}
        private ComboBox Choice(string label,string[] options,int selection,Action<int> changed)
        {
            LabelAt(label,16,rowY,136,28);var combo=new ComboBox{Left=160,Top=rowY+1,Width=270,Height=28,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Field,ForeColor=Green,FlatStyle=FlatStyle.Flat,AccessibleName=label};
            combo.Items.AddRange(options);combo.SelectedIndex=Math.Max(0,selection);combo.SelectedIndexChanged+=delegate{changed(combo.SelectedIndex);};body.Controls.Add(combo);rowY+=37;return combo;
        }
        private NumericUpDown Number(string label,int value,int min,int max)
        {
            LabelAt(label,16,rowY,136,28);var input=new NumericUpDown{Left=160,Top=rowY+1,Width=130,Height=28,Minimum=min,Maximum=max,Increment=10,Value=value,BackColor=Field,ForeColor=Green,AccessibleName=label};body.Controls.Add(input);rowY+=35;return input;
        }
        private void Toggle(string label,bool value,Action<bool> changed)
        {var check=new CheckBox{Text=label,Checked=value,Left=16,Top=rowY,Width=420,Height=28,ForeColor=Green,AccessibleName=label};check.CheckedChanged+=delegate{changed(check.Checked);};body.Controls.Add(check);rowY+=31;}
        protected override void OnFormClosed(FormClosedEventArgs e){clock.ResizeEnd-=OwnerResized;base.OnFormClosed(e);}
    }
}
