// Explicit allowlist for the public repository: no hosting configuration or local settings.
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'..');
const entries=[];
function add(file){const content=fs.readFileSync(path.join(root,file));entries.push({path:file.replaceAll('\\','/'),binary:/\.(zip|png|ico)$/.test(file),content:content.toString(/\.(zip|png|ico)$/.test(file)?'base64':'utf8')});}
function walk(dir){for(const entry of fs.readdirSync(path.join(root,dir),{withFileTypes:true})){const file=path.join(dir,entry.name);if(entry.isDirectory())walk(file);else add(file);}}
for(const file of ['.gitignore','README.md','SECURITY.md','package.json','preview.cjs','desktop/CistercianClock.cs','desktop/ClockWindow.cs','desktop/SettingsWindow.cs','desktop/WindowsIntegration.cs','desktop/install.ps1','desktop/clock.ico','desktop/create-icon.py','desktop/WidgetTests.cs','desktop/AssemblyInfo.cs','desktop/app.manifest','desktop/build.ps1','desktop/README.txt','scripts/public-source.cjs'])add(file);
walk('dist');walk('tests');
process.stdout.write(JSON.stringify(entries));
