using Guna.UI2.WinForms;
using INTERNAL_LOADER;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NULL_EXTERNAL
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer holdTimer;
        private bool keyIsDown = false;
        private Keys? boundKeyEnum = null;
        private DateTime holdStartTime;
        private bool _isListening = false;
        private string _boundKey = null;
        private GlobalKeyboardHook _hook;
        private HotkeyManager hotkeyManager;
        private bool formHidden = false;
        private System.Windows.Forms.Timer sniperTimeoutTimer;
        private DateTime lastToggleTime;
        bool isSniperScopeOn = false;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(699, 404);
            guna2Button1.ForeColor = Color.Red;
            FakeLagFileCheck();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            guna2Panel5.Visible = true;
            guna2Panel6.Visible = false;
            guna2Panel5.Location = new Point(70, 38);
            guna2Button1.ForeColor = Color.Red;
            guna2Button2.ForeColor = Color.DarkRed;
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            guna2Panel5.Visible = false;
            guna2Panel6.Visible = true;
            guna2Panel6.Location = new Point(70, 38);
            guna2Button1.ForeColor = Color.DarkRed;
            guna2Button2.ForeColor = Color.Red;
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public Form1()
        {
            InitializeComponent();

            particles = new ParticleSystem(this);

            holdTimer = new System.Windows.Forms.Timer();
            holdTimer.Interval = 50;
            holdTimer.Tick += (s, e) => WhileHolding();

            _hook = new GlobalKeyboardHook();
            _hook.KeyPressed += Hook_KeyPressed;
            _hook.KeyDown += Hook_KeyDown;
            _hook.KeyUp += Hook_KeyUp;

            sniperTimeoutTimer = new System.Windows.Forms.Timer();
            sniperTimeoutTimer.Interval = 20;
            sniperTimeoutTimer.Tick += SniperTimeoutTimer_Tick;
            sniperTimeoutTimer.Start();

            hotkeyManager = new HotkeyManager(this);

            hotkeyManager.SetHotkeyAction(guna2Button3, () =>
            {
                EnableAimbot();
            });

            hotkeyManager.SetHotkeyAction(guna2Button4, () =>
            {
                EnableCollider();
            });
            hotkeyManager.SetHotkeyAction(guna2Button5, async () =>
            {
                ToggleSniperScope();
            });
            hotkeyManager.SetHotkeyAction(guna2Button6, () =>
            {
                ToggleFeature(cameraFeature, "Camera");

                label52.Text = cameraFeature.Applied ? "ON" : "OFF";
            });

            hotkeyManager.SetHotkeyAction(guna2Button9, () =>
            {
                ToggleFeature(fastLandingFeature, "Fast Landing");

                label52.Text = fastLandingFeature.Applied ? "ON" : "OFF";
            });

            hotkeyManager.SetHotkeyAction(guna2Button10, ToggleHidden);
        }

        private void Hook_KeyPressed(Keys key)
        {
            switch (key)
            {
                case Keys.F1:
                    ToggleAimbotState(true);
                    break;

                case Keys.F2:
                    ToggleAimbotState(false);
                    break;

                case Keys.F3:
                    ToggleColliderState(true);
                    break;

                case Keys.F4:
                    ToggleColliderState(false);
                    break;
            }
        }

        private async Task InitializeAimbot()
        {
            try
            {
                label30.Text = "Initializing Aimbot...";

                await Aimbot.AimbotEnable();

                label30.Text = "Aimbot Initialized!";
            }
            catch
            {
                label30.Text = "Aimbot Init Failed!";
            }
        }

        private async Task InitializeCollider()
        {
            try
            {
                label30.Text = "Initializing Collider...";

                new Thread(Collider.Work)
                {
                    IsBackground = true
                }.Start();

                await Collider.InitAimbot();

                label30.Text = "Collider Initialized!";
            }
            catch
            {
                label30.Text = "Collider Init Failed!";
            }
        }

        private void ToggleAimbotState(bool state)
        {
            if (state)
            {
                label50.Text = "ON";

                Aimbot.Aimboton();

                label30.Text = "Aimbot ON!";

                if (!guna2ToggleSwitch16.Checked)
                    guna2ToggleSwitch16.Checked = true;
            }
            else
            {
                label50.Text = "OFF";

                Aimbot.Aimbotoff();

                label30.Text = "Aimbot OFF!";

                if (guna2ToggleSwitch16.Checked)
                    guna2ToggleSwitch16.Checked = false;
            }
        }

        private void ToggleColliderState(bool state)
        {
            if (state)
            {
                label17.Text = "ON";

                Collider.Enable();

                label30.Text = "Collider ON!";

                if (!guna2ToggleSwitch4.Checked)
                    guna2ToggleSwitch4.Checked = true;
            }
            else
            {
                label17.Text = "OFF";

                Collider.Disable();

                label30.Text = "Collider OFF!";

                if (guna2ToggleSwitch4.Checked)
                    guna2ToggleSwitch4.Checked = false;
            }
        }

        private async void EnableAimbot()
        {
            // Prevent enabling if Collider is active
            if (guna2ToggleSwitch5.Checked)
            {
                label30.Text = "Disable Collider First!";
                return;
            }

            label21.Text = "ON";

            await InitializeAimbot();

            // Disable Collider switch
            guna2ToggleSwitch5.Enabled = false;

            // Enable Aimbot switches
            guna2ToggleSwitch16.Enabled = true;
            guna2ToggleSwitch16.Checked = true;

            guna2ToggleSwitch6.Checked = true;

            ToggleAimbotState(true);
        }

        private async void EnableCollider()
        {
            // Prevent enabling if Aimbot is active
            if (guna2ToggleSwitch6.Checked)
            {
                label30.Text = "Disable Aimbot First!";
                return;
            }

            label19.Text = "ON";

            await InitializeCollider();

            // Disable Aimbot switch
            guna2ToggleSwitch6.Enabled = false;

            // Enable Collider switches
            guna2ToggleSwitch4.Enabled = true;
            guna2ToggleSwitch4.Checked = true;

            guna2ToggleSwitch5.Checked = true;

            ToggleColliderState(true);
        }

        private async void guna2ToggleSwitch6_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch6.Checked)
            {
                // Prevent enabling if Collider is already ON
                if (guna2ToggleSwitch5.Checked)
                {
                    guna2ToggleSwitch6.Checked = false;
                    label30.Text = "Disable Collider First!";
                    return;
                }

                label21.Text = "ON";

                await InitializeAimbot();

                // Disable Collider main switch
                guna2ToggleSwitch5.Enabled = false;

                // Enable switch16 only when switch6 is ON
                guna2ToggleSwitch16.Enabled = true;
                guna2ToggleSwitch16.Checked = true;

                ToggleAimbotState(true);
            }
            else
            {
                label21.Text = "OFF";

                Aimbot.Disable();

                ToggleAimbotState(false);

                // Re-enable Collider switch
                guna2ToggleSwitch5.Enabled = true;

                // Disable switch16 when switch6 is OFF
                guna2ToggleSwitch16.Checked = false;
                guna2ToggleSwitch16.Enabled = false;

                label30.Text = "Aimbot Disabled!";
            }
        }

        private void guna2ToggleSwitch16_CheckedChanged(object sender, EventArgs e)
        {
            // Prevent working if switch6 is OFF
            if (!guna2ToggleSwitch6.Checked)
            {
                guna2ToggleSwitch16.Checked = false;
                return;
            }

            ToggleAimbotState(guna2ToggleSwitch16.Checked);
        }

        private async void guna2ToggleSwitch5_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch5.Checked)
            {
                // Prevent enabling if Aimbot is already ON
                if (guna2ToggleSwitch6.Checked)
                {
                    guna2ToggleSwitch5.Checked = false;
                    label30.Text = "Disable Aimbot First!";
                    return;
                }

                label19.Text = "ON";

                await InitializeCollider();

                // Disable Aimbot main switch
                guna2ToggleSwitch6.Enabled = false;

                // Enable switch4 only when switch5 is ON
                guna2ToggleSwitch4.Enabled = true;
                guna2ToggleSwitch4.Checked = true;

                ToggleColliderState(true);

                label30.Text = "Collider Enabled!";
            }
            else
            {
                ToggleColliderState(false);

                label19.Text = "OFF";

                // Re-enable Aimbot switch
                guna2ToggleSwitch6.Enabled = true;

                // Disable switch4 when switch5 is OFF
                guna2ToggleSwitch4.Checked = false;
                guna2ToggleSwitch4.Enabled = false;

                label30.Text = "Collider Disabled!";
            }
        }

        private void guna2ToggleSwitch4_CheckedChanged(object sender, EventArgs e)
        {
            // Prevent working if switch5 is OFF
            if (!guna2ToggleSwitch5.Checked)
            {
                guna2ToggleSwitch4.Checked = false;
                return;
            }

            ToggleColliderState(guna2ToggleSwitch4.Checked);
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button3);
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button4);
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button5);
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button6);
        }

        private void guna2Button7_Click(object sender, EventArgs e)
        {
            _isListening = true;
            guna2Button7.Text = "...";
        }

        private void guna2Button9_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button9);
        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {
            hotkeyManager.BeginAssignHotkey(guna2Button10);
        }

        private void ToggleHidden()
        {
            this.Visible = !formHidden;
            formHidden = !formHidden;
        }

        private void FakeLagFileCheck()
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string targetFolder = Path.Combine(roaming, "Fake Lag Dlls");

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            Assembly asm = Assembly.GetExecutingAssembly();
            var resources = new (string resourceName, string outName)[]
            {
                    ("NULL_EXTERNAL.WinDivert.dll", "WinDivert.dll"),
                    ("NULL_EXTERNAL.WinDivert64.sys", "WinDivert64.sys")
            };

            bool allExist = true;
            foreach (var r in resources)
            {
                string outPath = Path.Combine(targetFolder, r.outName);
                if (!File.Exists(outPath))
                {
                    allExist = false;
                    break;
                }
            }

            if (allExist)
            {
                return;
            }

            foreach (var r in resources)
            {
                string outPath = Path.Combine(targetFolder, r.outName);
                if (!File.Exists(outPath))
                {
                    using (Stream rs = asm.GetManifestResourceStream(r.resourceName))
                    {
                        if (rs == null)
                        {
                            continue;
                        }
                        using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                        {
                            rs.CopyTo(fs);
                        }
                    }
                }
            }
        }

        private void OnPressStart()
        {
            if (guna2ToggleSwitch7.Checked == true)
            {
                holdStartTime = DateTime.Now;
            }
        }

        private void WhileHolding()
        {
            if (guna2ToggleSwitch7.Checked == false)
            {
                label52.Text = "Enable Fake Lag First!";
            }
            if (guna2ToggleSwitch7.Checked == true)
            {
                FakeLag.Start();
                double elapsedSeconds = (DateTime.Now - holdStartTime).TotalSeconds;
                label52.Text = $"FakeLag Started : {elapsedSeconds:F1} sec";
            }
        }

        private void OnRelease()
        {
            if (guna2ToggleSwitch7.Checked == true)
            {
                FakeLag.Stop();
                label52.Text = "FakeLag Stopped";
            }
        }

        private void Hook_KeyDown(Keys key)
        {
            if (boundKeyEnum.HasValue && key == boundKeyEnum.Value && !keyIsDown)
            {
                keyIsDown = true;
                OnPressStart();
                holdTimer.Start();
                return;
            }

            if (_isListening)
            {
                _boundKey = key.ToString();
                boundKeyEnum = key;
                _isListening = false;
                guna2Button7.Text = $"[{_boundKey}]";
                return;
            }
        }

        private void Hook_KeyUp(Keys key)
        {
            if (boundKeyEnum.HasValue && key == boundKeyEnum.Value && keyIsDown)
            {
                keyIsDown = false;
                holdTimer.Stop();
                OnRelease();
            }
        }

        Gaurav FastMemory = new Gaurav();

        public class MemoryFeature
        {
            public long Address = 0;
            public string OriginalBytes = null;
            public string Search = "";
            public string Replace = "";
            public bool Loaded = false;
            public bool Applied = false;
        }

        private MemoryFeature cameraFeature = new MemoryFeature
        {
            Search = "00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 80 7F 00 00 80 7F 00 00 80 7F 00 00 80 FF",

            Replace = "00 00 00 00 00 80 40 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 80 7F 00 00 80 7F 00 00 80 7F 00 00 80 FF"
        };

        private MemoryFeature fastLandingFeature = new MemoryFeature
        {
            Search = "00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 80 7F 00 00 80 7F 00 00 80 7F 00 00 80 FF",

            Replace = "00 00 00 00 00 00 FF 41 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 80 7F 00 00 80 7F 00 00 80 7F 00 00 80 FF"
        };

        private async Task<bool> LoadFeature(MemoryFeature feature)
        {
            try
            {
                string[] processName = { "HD-Player" };

                if (!FastMemory.SetProcess(processName))
                {
                    label24.Text = "Emulator Not Found!";
                    return false;
                }

                if (feature.Loaded)
                    return true;

                var result = await FastMemory.AoBScan(feature.Search);

                var matches = result.ToList();

                if (matches.Count == 0)
                {
                    label24.Text = "Pattern Not Found!";
                    return false;
                }

                feature.Address = matches[0];

                int len = feature.Search.Split(' ').Length;

                feature.OriginalBytes =
                    FastMemory.ReadString(feature.Address, len);

                feature.Loaded = true;

                return true;
            }
            catch
            {
                label24.Text = "Load Failed!";
                return false;
            }
        }

        private void ApplyFeature(MemoryFeature feature)
        {
            try
            {
                if (!feature.Loaded)
                    return;

                bool result =
                    FastMemory.AobReplace(
                        feature.Address,
                        feature.Replace);

                if (result)
                    feature.Applied = true;
            }
            catch
            {
            }
        }

        private void RestoreFeature(MemoryFeature feature)
        {
            try
            {
                if (!feature.Loaded)
                    return;

                if (feature.Address == 0)
                    return;

                if (string.IsNullOrEmpty(feature.OriginalBytes))
                    return;

                bool result =
                    FastMemory.AobReplace(
                        feature.Address,
                        feature.OriginalBytes);

                if (result)
                    feature.Applied = false;
            }
            catch
            {
            }
        }

        private void ToggleFeature(MemoryFeature feature, string name)
        {
            if (!feature.Applied)
            {
                ApplyFeature(feature);

                label52.Text = $"{name} Applied";
            }
            else
            {
                RestoreFeature(feature);

                label52.Text = $"{name} Restored";
            }
        }

        private async void guna2ToggleSwitch14_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch14.Checked)
            {
                label23.Text = "Loading Camera...";

                bool ok = await LoadFeature(cameraFeature);

                if (ok)
                {
                    label23.Text = "Camera Loaded";
                    label45.Text = "ON";
                }
                else
                {
                    guna2ToggleSwitch14.Checked = false;
                }
            }
            else
            {
                RestoreFeature(cameraFeature);

                label23.Text = "Camera OFF";
                label45.Text = "OFF";
            }
        }

        private async void guna2ToggleSwitch10_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch10.Checked)
            {
                label23.Text = "Loading Fast Landing...";

                bool ok = await LoadFeature(fastLandingFeature);

                if (ok)
                {
                    label23.Text = "Fast Landing Loaded";
                    label36.Text = "ON";
                }
                else
                {
                    guna2ToggleSwitch10.Checked = false;
                }
            }
            else
            {
                RestoreFeature(fastLandingFeature);

                label23.Text = "Fast Landing OFF";
                label36.Text = "OFF";
            }
        }

        private void guna2ToggleSwitch7_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch7.Checked)
            {
                label23.Text = "Fake Lag On";
                label34.Text = "ON";
            }
            else
            {
                label23.Text = "Fake Lag Off";
                label34.Text = "OFF";
            }
        }

        public class SniperFeature
        {
            public long Address = 0;
            public string OriginalBytes = null;
            public string Search = "";
            public string Replace = "";
            public bool Enabled = false;
        }

        private SniperFeature awmSwitch = new SniperFeature
        {
            Search = "01 00 00 00 C3 F5 E8 3F 01 00 00 00 00 00 00 00 C3 F5 E8 3F 00 00 00 00 C3 F5 E8 3F 00 00 80 3F 00 00 80 3F CD CC CC 3D 00 00 00 00 00 00 5C 43 00 00 90 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 3F 00 00 80 3E 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 0A D7 23 3F 9A 99 99 3F 00 00 80 3F 00 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 3F 00 00 00 00 00 00 40 3F 00 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 00 00 01 00 00 00 0A D7 23 3C CD CC CC 3D 9A 99 19 3F 1F 85 6B 3F 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3F 00 00 00 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 CD CC 4C 3E 01",

            Replace = "01 00 00 00 C3 F5 E8 3F 01 00 00 00 00 00 00 00 C3 F5 E8 3F 00 00 00 00 C3 F5 E8 3F 00 00 80 3F 00 00 80 3F CD CC CC 3D 00 00 00 00 00 00 5C 43 00 00 90 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 1E 00 00 80 1E 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 0A D7 23 3F 9A 99 99 3F 00 00 80 3F 00 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 00 3F 00 00 00 00 00 00 40 3F 00 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 00 00 01 00 00 00 0A D7 23 3C CD CC CC 3D 9A 99 19 3F 1F 85 6B 3F 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3F 00 00 00 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 CD CC 4C 3E 01"
        };

        private SniperFeature m82bSwitch = new SniperFeature
        {
            Search = "80 3F 00 00 80 3F 00 00 00 3F 00 00 00 00 00 00 5C 43 00 00 28 42 00 00 B4 42 78 00 00 00 00 00 00 00 9A 99 19 3F 00 00 80 3E 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 9A 99 19 3F CD CC 8C 3F 00 00 80 3F 00 00 00 00 66 66 66 3F 00 00 80 3F 00 00 80 3F",

            Replace = "80 3F 00 00 80 3F 00 00 00 3F 00 00 00 00 00 00 5C 43 00 00 28 42 00 00 B4 42 78 00 00 00 00 00 00 00 9A 99 19 1E 00 00 80 1E 00 00 00 00 04 00 00 00 00 00 80 3F 00 00 20 41 00 00 34 42 01 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 9A 99 19 3F CD CC 8C 3F 00 00 80 3F 00 00 00 00 66 66 66 3F 00 00 80 3F 00 00 80 3F"
        };

        private async Task<bool> EnableFeature(SniperFeature feature)
        {
            try
            {
                string[] processName = { "HD-Player" };

                if (!FastMemory.SetProcess(processName))
                {
                    label24.Text = "Emulator Not Found!";
                    return false;
                }

                if (feature.Enabled)
                    return true;

                if (feature.Address == 0)
                {
                    var result = await FastMemory.AoBScan(feature.Search);

                    var matches = result.ToList();

                    if (matches.Count == 0)
                    {
                        label24.Text = "Pattern Not Found!";
                        return false;
                    }

                    feature.Address = matches[0];

                    int len = feature.Search.Split(' ').Length;

                    feature.OriginalBytes =
                        FastMemory.ReadString(feature.Address, len);
                }

                FastMemory.AobReplace(
                    feature.Address,
                    feature.Replace);

                feature.Enabled = true;

                return true;
            }
            catch
            {
                label24.Text = "Enable Failed!";
                return false;
            }
        }

        private void DisableFeature(SniperFeature feature)
        {
            try
            {
                if (!feature.Enabled)
                    return;

                if (feature.Address != 0 &&
                    !string.IsNullOrEmpty(feature.OriginalBytes))
                {
                    FastMemory.AobReplace(
                        feature.Address,
                        feature.OriginalBytes);
                }

                feature.Enabled = false;
            }
            catch
            {
            }
        }

        private async void guna2ToggleSwitch12_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch12.Checked)
            {
                label24.Text = "Applying AWM...";

                bool ok = await EnableFeature(awmSwitch);

                if (ok)
                {
                    label24.Text = "AWM Enabled";
                    label39.Text = "ON";
                }
                else
                {
                    guna2ToggleSwitch12.Checked = false;
                }
            }
            else
            {
                DisableFeature(awmSwitch);

                label24.Text = "AWM Disabled";
                label39.Text = "OFF";
            }
        }

        private async void guna2ToggleSwitch11_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch11.Checked)
            {
                label24.Text = "Applying M82B...";

                bool ok = await EnableFeature(m82bSwitch);

                if (ok)
                {
                    label24.Text = "M82B Enabled";
                    label37.Text = "ON";
                }
                else
                {
                    guna2ToggleSwitch11.Checked = false;
                }
            }
            else
            {
                DisableFeature(m82bSwitch);

                label24.Text = "M82B Disabled";
                label37.Text = "OFF";
            }
        }

        List<long> sniperScopeAddresses = new List<long>();
        string originalBytes = "03 00 01 00 00 00 9A 99 99 3E FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F 01";
        string patchedBytes = "03 00 01 00 00 00 9A 99 99 3E FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F 01";
        string[] processname = { "HD-Player" };

        private async void guna2ToggleSwitch13_CheckedChanged(object sender, EventArgs e)
        {
            label24.Text = "Sniper Scope Applying ...";
            bool success = FastMemory.SetProcess(processname);

            if (!success)
            {
                return;
            }

            IEnumerable<long> results = await FastMemory.AoBScan(originalBytes);
            sniperScopeAddresses = results.ToList();

            if (sniperScopeAddresses.Count > 0)
            {
                label24.Text = "Sniper Scope Applied";
                label41.Text = "ON";
            }
            else
            {
                label24.Text = "Error!";
            }
        }

        private void SniperTimeoutTimer_Tick(object sender, EventArgs e)
        {
            if (isSniperScopeOn)
            {
                TimeSpan elapsed = DateTime.Now - lastToggleTime;
                if (elapsed.TotalSeconds >= 0)
                {
                    sniperscopeoff();
                    isSniperScopeOn = false;

                }
            }
        }

        private DateTime lastToggleAttempt = DateTime.MinValue;
        private readonly int toggleCooldownMs = 200;
        static int scopeoffdelay = 45;

        private async void ToggleSniperScope()
        {
            var now = DateTime.Now;
            if ((now - lastToggleAttempt).TotalMilliseconds < toggleCooldownMs)
                return;

            lastToggleAttempt = now;

            await Task.Delay(10);

            if (!isSniperScopeOn)
            {
                sniperscopeon();
                await Task.Delay(scopeoffdelay);
                sniperscopeoff();
            }
            isSniperScopeOn = !isSniperScopeOn;
            lastToggleTime = now;
        }

        private void sniperscopeon()
        {
            if (sniperScopeAddresses.Count == 0)
                return;

            foreach (long addr in sniperScopeAddresses)
            {
                FastMemory.AobReplace(addr, patchedBytes);
            }
            label52.Text = "Sniper Turned On";
            Invoke((Action)(() =>
            {

            }));
        }

        private void sniperscopeoff()
        {
            if (sniperScopeAddresses.Count == 0)
                return;

            foreach (long addr in sniperScopeAddresses)
            {
                FastMemory.AobReplace(addr, originalBytes);
            }
            label52.Text = "Sniper Restored";
            Invoke((Action)(() =>
            {

            }));
        }

        private void guna2ToggleSwitch15_CheckedChanged(object sender, EventArgs e)
        {
            if (!guna2ToggleSwitch15.Checked)
            {
                DisableAllSnipers();
            }
        }

        private void DisableAllSnipers()
        {
            DisableFeature(awmSwitch);
            DisableFeature(m82bSwitch);
            sniperscopeoff();

            guna2ToggleSwitch12.Checked = false;
            guna2ToggleSwitch11.Checked = false;
            guna2ToggleSwitch13.Checked = false;

            label39.Text = "OFF";
            label37.Text = "OFF";
            label48.Text = "OFF";
            label41.Text = "OFF";

            label24.Text = "All Snipers Disabled";
        }

        public static bool Streaming;
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        private void guna2ToggleSwitch1_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch1.Checked)
            {
                base.ShowInTaskbar = false;
                Form1.Streaming = true;
                Form1.SetWindowDisplayAffinity(base.Handle, 17U);
            }
            else
            {
                base.ShowInTaskbar = true;
                Form1.Streaming = false;
                Form1.SetWindowDisplayAffinity(base.Handle, 0U);
            }
        }

        private ParticleSystem particles;

        private void guna2ToggleSwitch2_CheckedChanged(
            object sender,
            EventArgs e)
        {
            if (guna2ToggleSwitch2.Checked)
            {
                label24.Text = "Particles Enabled";

                label12.Text = "ON";

                particles.Resume();
            }
            else
            {
                label24.Text = "Particles Disabled";

                label12.Text = "OFF";

                particles.Pause();
            }
        }

        private void guna2ToggleSwitch3_CheckedChanged(object sender, EventArgs e)
        {
            MessageBox.Show(
    "This feature is locked.\n\nPurchase the premium version to access all features.",
    "Premium Access",
    MessageBoxButtons.OK,
    MessageBoxIcon.Warning
);
        }

        private const string EmulatorProcess = "HD-Player";

        private bool InjectDll(string resourceName, string dllName)
        {
            try
            {
                string tempDllPath = Path.Combine(Path.GetTempPath(), dllName);

                ExtractEmbeddedResourceToFile(resourceName, tempDllPath);

                Process[] targetProcesses = Process.GetProcessesByName(EmulatorProcess);

                if (targetProcesses.Length == 0)
                {
                    label30.Text = "Emulator Not Found!";
                    return false;
                }

                Process targetProcess = targetProcesses[0];

                IntPtr hProcess = OpenProcess(
                    PROCESS_CREATE_THREAD |
                    PROCESS_QUERY_INFORMATION |
                    PROCESS_VM_OPERATION |
                    PROCESS_VM_WRITE |
                    PROCESS_VM_READ,
                    false,
                    targetProcess.Id);

                if (hProcess == IntPtr.Zero)
                {
                    label30.Text = "OpenProcess Failed!";
                    return false;
                }

                IntPtr loadLibraryAddr = GetProcAddress(
                    GetModuleHandle("kernel32.dll"),
                    "LoadLibraryA");

                if (loadLibraryAddr == IntPtr.Zero)
                {
                    label30.Text = "LoadLibrary Failed!";
                    return false;
                }

                IntPtr allocMemAddress = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (IntPtr)tempDllPath.Length,
                    MEM_COMMIT,
                    PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                {
                    label30.Text = "Memory Allocation Failed!";
                    return false;
                }

                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(tempDllPath);

                bool writeResult = WriteProcessMemory(
                    hProcess,
                    allocMemAddress,
                    bytes,
                    (uint)bytes.Length,
                    out _);

                if (!writeResult)
                {
                    label30.Text = "WriteProcessMemory Failed!";
                    return false;
                }

                IntPtr thread = CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    loadLibraryAddr,
                    allocMemAddress,
                    0,
                    IntPtr.Zero);

                if (thread == IntPtr.Zero)
                {
                    label30.Text = "Injection Failed!";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                label30.Text = ex.Message;
                return false;
            }
        }

        private void InjectChams()
        {
            bool result = InjectDll(
                "NULL_EXTERNAL.LOCATION.dll",
                "LOCATION.dll");

            label30.Text = result
                ? "Chams Injected"
                : "Chams Injection Failed!";
        }

        private void InjectEsp()
        {
            bool esp1 = InjectDll(
                "NULL_EXTERNAL.Client.dll",
                "Client.dll");

            bool esp2 = InjectDll(
                "NULL_EXTERNAL.cimgui.dll",
                "cimgui.dll");

            bool esp3 = InjectDll(
                "NULL_EXTERNAL.AotBst.dll",
                "AotBst.dll");

            label30.Text =
                (esp1 && esp2 && esp3)
                ? "ESP Injected"
                : "ESP Injection Failed!";
        }

        private void guna2ToggleSwitch9_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch9.Checked)
            {
                InjectChams();
            }
        }

        private void guna2ToggleSwitch8_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch8.Checked)
            {
                InjectEsp();
            }

            //            MessageBox.Show(
            //"This feature is locked.\n\nPurchase the premium version to access all features.",
            //"Premium Access",
            //MessageBoxButtons.OK,
            //MessageBoxIcon.Warning
            //);
        }

        private void guna2ToggleSwitch17_CheckedChanged(object sender, EventArgs e)
        {
            if (!guna2ToggleSwitch17.Checked)
                return;

            string resourceName = "NULL_EXTERNAL.opengl32.dll";

            string[] targetFolders =
            {
        @"C:\Program Files\BlueStacks_msi5",
        @"C:\Program Files\BlueStacks_nxt"
    };

            bool installedFound = false;
            bool copied = false;

            foreach (string folder in targetFolders)
            {
                try
                {
                    if (!Directory.Exists(folder))
                        continue;

                    installedFound = true;

                    string targetPath = Path.Combine(folder, "opengl32.dll");

                    if (File.Exists(targetPath))
                        File.SetAttributes(targetPath, FileAttributes.Normal);

                    ExtractEmbeddedResourceToFile(resourceName, targetPath);

                    copied = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (!installedFound)
                label30.Text = "BlueStacks Not Found!";
            else if (copied)
                label30.Text = "Chams Fixed";
            else
                label30.Text = "Chams Fix Failed!";
        }

        private static void ExtractEmbeddedResourceToFile(string resourceName, string outputPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                throw new ArgumentException($"Resource '{resourceName}' not found.");

            using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            stream.CopyTo(fileStream);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttribute,
            IntPtr dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            IntPtr lpThreadId);

        const uint PROCESS_CREATE_THREAD = 0x2;
        const uint PROCESS_QUERY_INFORMATION = 0x400;
        const uint PROCESS_VM_OPERATION = 0x8;
        const uint PROCESS_VM_WRITE = 0x20;
        const uint PROCESS_VM_READ = 0x10;
        const uint MEM_COMMIT = 0x1000;
        const uint PAGE_READWRITE = 4;
    }
}
