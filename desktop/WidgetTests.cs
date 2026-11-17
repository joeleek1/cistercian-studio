using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CistercianStudio
{
    public static class WidgetTests
    {
        private static void Check(bool value,string message){if(!value)throw new Exception(message);}
        private static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
        public static void Run()
        {
            var date=new DateTime(2026,11,17);
            Check(DateDisplay.Text(date,"DMY")=="17/11/2026","DMY date");
            Check(DateDisplay.Text(date,"MDY")=="11/17/2026","MDY date");
            Check(DateDisplay.Text(date,"YMD")=="2026-11-17","ISO date");
            Check(DateDisplay.Text(date,"Long")=="Tuesday, 17 November 2026","Long date");
            Check(DateDisplay.Values(date,"YMD").SequenceEqual(new[]{2026,11,17}),"Year-first numeral order");
            Check(DateDisplay.Values(date,"MDY").SequenceEqual(new[]{11,17,2026}),"Month-first numeral order");
            foreach(var size in new[]{new Size(240,150),new Size(600,360),new Size(1600,1200)})
                foreach(string style in new[]{"Symbols","Text","Both"})
                    foreach(bool labels in new[]{false,true})
                    {
                        var settings=new ClockSettings{Date=true,DateStyle=style,Labels=labels};var layout=ClockLayout.Create(size,settings);
                        Check(layout.Time.Left>=0&&layout.Time.Right<=size.Width,"Time width bounds");
                        Check(layout.Time.Bottom<size.Height-30,"Time height bounds");
                        Check(layout.Date.IsEmpty||layout.Date.Bottom<size.Height-30,"Date height bounds");
                        Check(layout.DateText.IsEmpty||layout.DateText.Bottom<size.Height-30,"Date text height bounds");
                        Check(new Rectangle(Point.Empty,size).Contains(ClockLayout.Cog(size)),"Cog within bounds");
                    }
            string temp=Path.Combine(Path.GetTempPath(),"cistercian-v11-"+Guid.NewGuid().ToString("N")+".settings");
            try
            {
                File.WriteAllText(temp,"Theme=Amber\nDate=True\nSize=2\nOpacity=70");
                var legacy=ClockSettings.Load(temp);Check(legacy.Date&&legacy.Theme=="Amber"&&legacy.Width==0&&legacy.DateFormat=="DMY","Version 1 settings migration");
                var custom=new ClockSettings{Width=638,Height=382,TransparentBackground=true,Date=true,DateStyle="Both",DateFormat="YMD",Opacity=55};custom.Save(temp);
                var loaded=ClockSettings.Load(temp);Check(loaded.Width==638&&loaded.Height==382&&loaded.TransparentBackground&&loaded.DateStyle=="Both"&&loaded.DateFormat=="YMD"&&loaded.Opacity==55,"New settings round trip");
                File.WriteAllText(temp,"Width=-1\nHeight=999999\nOpacity=0\nDateStyle=garbage\nDateFormat=garbage");
                var invalid=ClockSettings.Load(temp);Check(invalid.Width==240&&invalid.Height==1200&&invalid.Opacity==40&&invalid.DateStyle=="Symbols"&&invalid.DateFormat=="DMY","Invalid settings bounds");
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
            var viewSettings=new ClockSettings();
            using(var clock=new ClockWindow(viewSettings,true))
            {
                clock.ClientSize=new Size(670,340);
                Check(clock.SettingsBounds==ClockLayout.Cog(clock.ClientSize),"Cog follows resize");
                using(var bitmap=new Bitmap(670,340))using(var graphics=Graphics.FromImage(bitmap))clock.RenderClock(graphics,new DateTime(2026,11,17,19,48,36));
                using(var dialog=new SettingsWindow(clock,viewSettings))
                {
                    var controls=Descendants(dialog).ToArray();
                    controls.OfType<CheckBox>().Single(c=>c.AccessibleName=="Show date").Checked=true;
                    controls.OfType<ComboBox>().Single(c=>c.AccessibleName=="Date display").SelectedIndex=2;
                    controls.OfType<ComboBox>().Single(c=>c.AccessibleName=="Date format").SelectedIndex=2;
                    controls.OfType<TrackBar>().Single().Value=35;
                    controls.OfType<CheckBox>().Single(c=>c.AccessibleName=="Transparent background (floating symbols)").Checked=true;
                    controls.OfType<NumericUpDown>().Single(c=>c.AccessibleName=="Width (pixels)").Value=520;
                    controls.OfType<NumericUpDown>().Single(c=>c.AccessibleName=="Height (pixels)").Value=360;
                    Check(viewSettings.Date&&viewSettings.DateStyle=="Both"&&viewSettings.DateFormat=="YMD","Settings date controls apply");
                    Check(Math.Abs(clock.Opacity-.65)<.01&&clock.TransparencyKey==clock.BackColor,"Transparency controls apply");
                    Check(clock.ClientSize==new Size(520,360)&&clock.SettingsBounds==ClockLayout.Cog(clock.ClientSize),"Size controls apply");
                    controls.OfType<CheckBox>().Single(c=>c.AccessibleName=="Show decimal labels below symbols").Checked=true;
                    clock.ClientSize=new Size(240,150);
                    clock.ApplySettings();
                    Check(clock.ClientSize.Height>=210,"Labelled date rows remain readable at minimum size");
                    Check(controls.OfType<NumericUpDown>().Single(c=>c.AccessibleName=="Height (pixels)").Minimum==210,"Settings respect the readable minimum size");
                }
            }
        }
    }
}
