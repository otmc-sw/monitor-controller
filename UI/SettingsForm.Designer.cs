namespace monitor_controller.UI;

public partial class SettingsForm
{
    private void InitializeComponent(
        out ComboBox monitorComboBox,
        out TrackBar manualBrightnessTrackBar,
        out TrackBar manualContrastTrackBar,
        out Label manualBrightnessValueLabel,
        out Label manualContrastValueLabel,
        out ListBox profileList,
        out TextBox timeTextBox,
        out TrackBar profileBrightnessTrackBar,
        out TrackBar profileContrastTrackBar,
        out Label profileBrightnessValueLabel,
        out Label profileContrastValueLabel,
        out Button addButton,
        out Button editButton,
        out Button deleteButton,
        out Button applyButton,
        out Button resetButton,
        out Label statusLabel,
        out Button saveButton)
    {
        Text = "OTMC Monitor Controller";
        Width = 820;
        Height = 840;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        BackColor = Color.FromArgb(248, 249, 250); // Màu nền sáng hiện đại hơn

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24),
            AutoScroll = true
        };
        
        // Thiết lập chiều cao chuẩn cho từng Card
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F)); // 1. Display Selection
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F)); // 2. Manual Control
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 360F)); // 3. Scheduled Profiles
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // 4. Status & Save
        
        Controls.Add(mainLayout);

        var regularFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        var boldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        var sectionTitleFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        var subTextFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        // --- 1. DISPLAY SELECTION CARD ---
        var displayCard = CreateCardPanel();
        var displayLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        displayLayout.Controls.Add(new Label
        {
            Text = "Display Device",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(33, 37, 41),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        }, 0, 0);
        displayLayout.Controls.Add(new Label
        {
            Text = "Select the target monitor to control",
            Font = subTextFont,
            ForeColor = Color.FromArgb(108, 117, 125),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);

        monitorComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Font = regularFont,
            Height = 32
        };
        displayLayout.Controls.Add(monitorComboBox, 0, 2);
        displayCard.Controls.Add(displayLayout);
        mainLayout.Controls.Add(displayCard, 0, 0);

        // --- 2. MANUAL CONTROL CARD ---
        var manualCard = CreateCardPanel();
        var manualLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5
        };
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var manualTitle = new Label
        {
            Text = "Manual Control",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(33, 37, 41),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        manualLayout.Controls.Add(manualTitle, 0, 0);
        manualLayout.SetColumnSpan(manualTitle, 2);

        // Brightness row
        manualLayout.Controls.Add(new Label { Text = "Brightness", Font = regularFont, ForeColor = Color.FromArgb(73, 80, 87), AutoSize = true }, 0, 1);
        manualBrightnessValueLabel = new Label { Text = "60", Font = boldFont, ForeColor = Color.FromArgb(33, 37, 41), AutoSize = true, Anchor = AnchorStyles.Right };
        manualLayout.Controls.Add(manualBrightnessValueLabel, 1, 1);

        manualBrightnessTrackBar = CreateTrackBar();
        manualLayout.Controls.Add(manualBrightnessTrackBar, 0, 2);
        manualLayout.SetColumnSpan(manualBrightnessTrackBar, 2);

        // Contrast row
        manualLayout.Controls.Add(new Label { Text = "Contrast", Font = regularFont, ForeColor = Color.FromArgb(73, 80, 87), AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 3);
        manualContrastValueLabel = new Label { Text = "30", Font = boldFont, ForeColor = Color.FromArgb(33, 37, 41), AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) };
        manualLayout.Controls.Add(manualContrastValueLabel, 1, 3);

        manualContrastTrackBar = CreateTrackBar();
        manualLayout.Controls.Add(manualContrastTrackBar, 0, 4);
        manualLayout.SetColumnSpan(manualContrastTrackBar, 2);

        manualCard.Controls.Add(manualLayout);
        mainLayout.Controls.Add(manualCard, 0, 1);

        // --- 3. SCHEDULED PROFILES CARD ---
        var profileCard = CreateCardPanel();
        var profileCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        profileCardLayout.Controls.Add(new Label
        {
            Text = "Scheduled Profiles",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(33, 37, 41),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);

        var profileSplitLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F)); 
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Left side: ListBox
        profileList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = regularFont,
            Margin = new Padding(0, 0, 12, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        profileSplitLayout.Controls.Add(profileList, 0, 0);

        // Right side: Profile Editor
        var profileEditorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7
        };
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        profileEditorPanel.Controls.Add(new Label { Text = "Time (HH:mm)", Font = regularFont, ForeColor = Color.FromArgb(73, 80, 87), AutoSize = true }, 0, 0);
        timeTextBox = new TextBox { Dock = DockStyle.Fill, Text = "08:00", Font = regularFont, Margin = new Padding(0, 2, 0, 8), Height = 26 };
        profileEditorPanel.Controls.Add(timeTextBox, 0, 1);
        profileEditorPanel.SetColumnSpan(timeTextBox, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Profile Brightness", Font = regularFont, ForeColor = Color.FromArgb(73, 80, 87), AutoSize = true }, 0, 2);
        profileBrightnessValueLabel = new Label { Text = "50", Font = boldFont, ForeColor = Color.FromArgb(33, 37, 41), AutoSize = true, Anchor = AnchorStyles.Right };
        profileEditorPanel.Controls.Add(profileBrightnessValueLabel, 1, 2);

        profileBrightnessTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(profileBrightnessTrackBar, 0, 3);
        profileEditorPanel.SetColumnSpan(profileBrightnessTrackBar, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Profile Contrast", Font = regularFont, ForeColor = Color.FromArgb(73, 80, 87), AutoSize = true, Margin = new Padding(0, 4, 0, 0) }, 0, 4);
        profileContrastValueLabel = new Label { Text = "50", Font = boldFont, ForeColor = Color.FromArgb(33, 37, 41), AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 4, 0, 0) };
        profileEditorPanel.Controls.Add(profileContrastValueLabel, 1, 4);

        profileContrastTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(profileContrastTrackBar, 0, 5);
        profileEditorPanel.SetColumnSpan(profileContrastTrackBar, 2);

        // Action Buttons: Add, Apply, Edit(Save), Delete
        var actionButtonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 8, 0, 0)
        };
        addButton = CreateActionButton("Add", 62, 28, false);
        applyButton = CreateActionButton("Apply", 62, 28, false);
        editButton = CreateActionButton("Save", 62, 28, false);
        deleteButton = CreateActionButton("Delete", 62, 28, false);
        
        // Thêm khoảng cách nhỏ giữa các nút
        addButton.Margin = new Padding(0, 0, 4, 0);
        applyButton.Margin = new Padding(0, 0, 4, 0);
        editButton.Margin = new Padding(0, 0, 4, 0);
        deleteButton.Margin = new Padding(0, 0, 0, 0);

        actionButtonsFlow.Controls.AddRange(new Control[] { addButton, applyButton, editButton, deleteButton });
        profileEditorPanel.Controls.Add(actionButtonsFlow, 0, 6);
        profileEditorPanel.SetColumnSpan(actionButtonsFlow, 2);

        profileSplitLayout.Controls.Add(profileEditorPanel, 1, 0);
        profileCardLayout.Controls.Add(profileSplitLayout, 0, 1);
        profileCard.Controls.Add(profileCardLayout);
        mainLayout.Controls.Add(profileCard, 0, 2);

        // --- 4. STATUS BAR & SAVE / RESET BUTTONS ---
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        statusLabel = new Label
        {
            Text = "● Ready",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(40, 167, 69) // Màu xanh trạng thái tích cực
        };
        
        var bottomButtonsFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty
        };

        resetButton = CreateActionButton("Reset Default", 110, 36, false);
        resetButton.Margin = new Padding(0, 0, 8, 0);

        saveButton = CreateActionButton("Save Configuration", 160, 36, true);

        bottomButtonsFlow.Controls.Add(resetButton);
        bottomButtonsFlow.Controls.Add(saveButton);

        bottomPanel.Controls.Add(statusLabel, 0, 0);
        bottomPanel.Controls.Add(bottomButtonsFlow, 1, 0);
        mainLayout.Controls.Add(bottomPanel, 0, 3);
    }

    private static TrackBar CreateTrackBar()
    {
        var trackBar = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 1,
            Dock = DockStyle.Fill,
            Height = 32
        };

        trackBar.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && s is TrackBar tb)
            {
                const int padding = 12;
                int usableWidth = tb.Width - (2 * padding);
                if (usableWidth > 0)
                {
                    double ratio = (double)(e.X - padding) / usableWidth;
                    int value = tb.Minimum + (int)Math.Round(ratio * (tb.Maximum - tb.Minimum));
                    tb.Value = Math.Clamp(value, tb.Minimum, tb.Maximum);
                }
            }
        };

        return trackBar;
    }

    private static Panel CreateCardPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.White
        };
    }

    private static Button CreateActionButton(string text, int width, int height, bool isPrimary)
    {
        var btn = new Button
        {
            Text = text,
            Height = height,
            Width = width,
            Font = new Font("Segoe UI", 9.5F, isPrimary ? FontStyle.Bold : FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };

        if (isPrimary)
        {
            btn.BackColor = Color.FromArgb(0, 120, 212); // Xanh Windows Accent
            btn.ForeColor = Color.White;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);
        }
        else
        {
            btn.BackColor = Color.FromArgb(241, 243, 245);
            btn.ForeColor = Color.FromArgb(33, 37, 41);
            btn.FlatAppearance.BorderColor = Color.FromArgb(222, 226, 230);
            
            // Hiệu ứng hover nhẹ cho nút phụ
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(222, 226, 230);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(241, 243, 245);
        }

        return btn;
    }
}