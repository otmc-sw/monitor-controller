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
        out Label statusLabel,
        out Button saveButton)
    {
        Text = "OTMC Monitor Controller";
        Width = 780;
        Height = 800;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        BackColor = Color.FromArgb(243, 243, 243);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20),
            AutoScroll = true
        };
        
        // Cố định chiều cao cho từng dòng card trong mainLayout (thay đổi giá trị số pixel theo ý muốn)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // 1. Display Selection Card (Cố định chiều cao 110px)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F)); // 2. Manual Control Card (Cố định chiều cao 220px)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 340F)); // 3. Scheduled Profiles Card (Cố định chiều cao 340px)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // 4. Status Bar & Save Button (Cố định chiều cao 60px)
        
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
            RowCount = 3,
            AutoSize = false // Tắt tự động giãn kích thước để tuân thủ theo kích thước cố định
        };
        displayLayout.Controls.Add(new Label
        {
            Text = "Display",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        displayLayout.Controls.Add(new Label
        {
            Text = "Target monitor",
            Font = subTextFont,
            ForeColor = Color.FromArgb(110, 110, 110),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
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
            RowCount = 5,
            AutoSize = false
        };
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        manualLayout.Controls.Add(new Label
        {
            Text = "Manual Control",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);
        manualLayout.SetColumnSpan(manualLayout.GetControlFromPosition(0, 0)!, 2);

        // Brightness row
        manualLayout.Controls.Add(new Label { Text = "Brightness", Font = regularFont, AutoSize = true }, 0, 1);
        manualBrightnessValueLabel = new Label { Text = "60", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right };
        manualLayout.Controls.Add(manualBrightnessValueLabel, 1, 1);

        manualBrightnessTrackBar = CreateTrackBar();
        manualLayout.Controls.Add(manualBrightnessTrackBar, 0, 2);
        manualLayout.SetColumnSpan(manualBrightnessTrackBar, 2);

        // Contrast row
        manualLayout.Controls.Add(new Label { Text = "Contrast", Font = regularFont, AutoSize = true, Margin = new Padding(0, 4, 0, 0) }, 0, 3);
        manualContrastValueLabel = new Label { Text = "30", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 4, 0, 0) };
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
            RowCount = 2,
            AutoSize = false
        };
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        profileCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        profileCardLayout.Controls.Add(new Label
        {
            Text = "Scheduled Profiles",
            Font = sectionTitleFont,
            ForeColor = Color.FromArgb(32, 32, 32),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var profileSplitLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = false
        };
        // Cố định tỷ lệ hoặc kích thước các cột bên trong phần Profile (ví dụ: Cột trái rộng cố định 250px, cột phải phần trăm còn lại)
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F)); 
        profileSplitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Left side: ListBox
        profileList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = regularFont,
            Margin = new Padding(0, 0, 10, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        profileSplitLayout.Controls.Add(profileList, 0, 0);

        // Right side: Profile Editor
        var profileEditorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            AutoSize = false
        };
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        profileEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        profileEditorPanel.Controls.Add(new Label { Text = "Time (HH:mm)", Font = regularFont, AutoSize = true }, 0, 0);
        timeTextBox = new TextBox { Dock = DockStyle.Fill, Text = "08:00", Font = regularFont, Margin = new Padding(0, 2, 0, 8) };
        profileEditorPanel.Controls.Add(timeTextBox, 0, 1);
        profileEditorPanel.SetColumnSpan(timeTextBox, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Brightness", Font = regularFont, AutoSize = true }, 0, 2);
        profileBrightnessValueLabel = new Label { Text = "50", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right };
        profileEditorPanel.Controls.Add(profileBrightnessValueLabel, 1, 2);

        profileBrightnessTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(profileBrightnessTrackBar, 0, 3);
        profileEditorPanel.SetColumnSpan(profileBrightnessTrackBar, 2);

        profileEditorPanel.Controls.Add(new Label { Text = "Contrast", Font = regularFont, AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 4);
        profileContrastValueLabel = new Label { Text = "50", Font = boldFont, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 0) };
        profileEditorPanel.Controls.Add(profileContrastValueLabel, 1, 4);

        profileContrastTrackBar = CreateTrackBar();
        profileEditorPanel.Controls.Add(profileContrastTrackBar, 0, 5);
        profileEditorPanel.SetColumnSpan(profileContrastTrackBar, 2);

        // Action Buttons: Add, Apply, Edit, Delete
        var actionButtonsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 10, 0, 0)
        };
        addButton = CreateStandardButton("Add", 70, 30);
        applyButton = CreateStandardButton("Apply", 70, 30);
        editButton = CreateStandardButton("Edit", 70, 30);
        deleteButton = CreateStandardButton("Delete", 70, 30);
        actionButtonsFlow.Controls.AddRange(new Control[] { addButton, applyButton, editButton, deleteButton });
        profileEditorPanel.Controls.Add(actionButtonsFlow, 0, 6);
        profileEditorPanel.SetColumnSpan(actionButtonsFlow, 2);

        profileSplitLayout.Controls.Add(profileEditorPanel, 1, 0);
        profileCardLayout.Controls.Add(profileSplitLayout, 0, 1);
        profileCard.Controls.Add(profileCardLayout);
        mainLayout.Controls.Add(profileCard, 0, 2);

        // --- 4. STATUS BAR & SAVE BUTTON ---
        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
            AutoSize = false
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        statusLabel = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
            ForeColor = Color.FromArgb(90, 90, 90)
        };
        saveButton = CreateStandardButton("Save Configuration", 150, 36);

        bottomPanel.Controls.Add(statusLabel, 0, 0);
        bottomPanel.Controls.Add(saveButton, 1, 0);
        mainLayout.Controls.Add(bottomPanel, 0, 3);
    }

    private static TrackBar CreateTrackBar()
    {
        return new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 1,
            Dock = DockStyle.Fill,
            Height = 38
        };
    }

    private static Panel CreateCardPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Button CreateStandardButton(string text, int width, int height)
    {
        var btn = new Button
        {
            Text = text,
            Height = height,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            UseVisualStyleBackColor = true
        };
        if (width > 0) btn.Width = width;
        return btn;
    }
}