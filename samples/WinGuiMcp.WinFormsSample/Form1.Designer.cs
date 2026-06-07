namespace WinGuiMcp.WinFormsSample;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;
    private Button clickButton = null!;
    private TextBox inputTextBox = null!;
    private Label outputLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        clickButton = new Button();
        inputTextBox = new TextBox();
        outputLabel = new Label();
        SuspendLayout();

        clickButton.Location = new Point(24, 24);
        clickButton.Name = "clickButton";
        clickButton.Size = new Size(120, 32);
        clickButton.TabIndex = 0;
        clickButton.Text = "Click Me";
        clickButton.UseVisualStyleBackColor = true;
        clickButton.Click += ClickButton_Click;

        inputTextBox.Location = new Point(24, 72);
        inputTextBox.Name = "inputTextBox";
        inputTextBox.Size = new Size(240, 23);
        inputTextBox.TabIndex = 1;
        inputTextBox.AccessibleName = "InputTextBox";
        inputTextBox.AccessibleRole = AccessibleRole.Text;

        outputLabel.AutoSize = true;
        outputLabel.Location = new Point(24, 112);
        outputLabel.Name = "outputLabel";
        outputLabel.Size = new Size(38, 15);
        outputLabel.TabIndex = 2;
        outputLabel.Text = "Ready";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(400, 180);
        Controls.Add(outputLabel);
        Controls.Add(inputTextBox);
        Controls.Add(clickButton);
        Name = "Form1";
        Text = "WinGuiMcp WinForms Sample";
        ResumeLayout(false);
        PerformLayout();
    }

    private void ClickButton_Click(object? sender, EventArgs e)
    {
        var text = string.IsNullOrWhiteSpace(inputTextBox.Text) ? "Clicked!" : inputTextBox.Text;
        outputLabel.Text = $"Output: {text}";
    }
}
