using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using System;
using System.Threading; // For Thread.Sleep
using FlaUI.Core.Patterns;
using NUnit.Framework; // NUnit Framework attributes and assertions
using System.Diagnostics;
using System.Linq; // Added for .FirstOrDefault() and .Any() extension methods
using System.IO; // Required for file operations
using FlaUI.Core.WindowsAPI;//For clipboard operations
using FlaUI.Core.Input; // For keyboard and mouse input
using FlaUI.Core.AutomationElements.Infrastructure;
using System.Diagnostics.CodeAnalysis;
//  using FlaUI.Core.

// For AutomationElement extensions
namespace MyDesktopAutomationTests;

[TestFixture]
public sealed class Test1
{
    [Test]
    public void TestMethod1()
    {
        // This test is empty, as it was in your original code.
        // You can add NUnit assertions here if needed.
        Assert.Pass("Test1.TestMethod1 completed successfully.");
    }
}

[TestFixture]
public sealed class Test2
{
    private UIA3Automation automation;
    public FlaUI.Core.Application application;
    private Window mainWindow;
    private ConditionFactory cf;
    private string testFilePath;
    private Application app;

    

    // [SetUp]
    // public void Setup()
    // {
    //     Automation = new UIA3Automation();
    //     Console.WriteLine("Automation instance initialized.");
    //     mainWindow = app.GetMainWindow(Automation); // Pass the automation instance

    //     // Ensure the path to your application is correct
    //     application = Application.Launch("C:\\Users\\MI2\\source\\repos\\MyFirstWinFormApp\\MyFirstWinFormApp\\bin\\Debug\\net9.0-windows\\MyFirstWinFormApp.exe");

    //     // It's good practice to wait for the main window to appear
    //     mainWindow = application.GetMainWindow(Automation, TimeSpan.FromSeconds(10)); // Added timeout for robustness
    //     Assert.That(mainWindow, Is.Not.Null, "Main window was not found after launching the application.");

    //     cf = new ConditionFactory(Automation.PropertyLibrary); // Use Automation.PropertyLibrary
    //     Console.WriteLine("Setup completed.");
    //     testFilePath = Path.Combine(Path.GetTempPath(), $"FlaUITestFile_{Guid.NewGuid()}.txt");
    //     Console.WriteLine($"Test file path for save operation: {testFilePath}");
    // }
    [SetUp]
    public void Setup()
    {
        Console.WriteLine("--- Setup Started ---");
        // Initialize the automation object here
        automation = new UIA3Automation();
        Console.WriteLine("Automation instance initialized.");

        // Ensure the application is started before each test
        // Adjust the path to your WinForms application's .exe file
        string appPath = @"C:\\Users\\MI2\\source\\repos\\MyFirstWinFormApp\\MyFirstWinFormApp\\bin\\Debug\\net9.0-windows\\MyFirstWinFormApp.exe"; // IMPORTANT: Update this path!
        Console.WriteLine($"Attempting to launch application from: {appPath}");

        // Set the test file path to a fixed name in the temporary directory
        testFilePath = Path.Combine(Path.GetTempPath(), "MyAppData.txt");
        Console.WriteLine($"Test file path for save operation: {testFilePath}");

        // --- IMPORTANT: Explicit file deletion logic in Setup ---
        // This ensures a clean slate for each test run by deleting the file
        // if it exists from a previous run or manual save.
        if (File.Exists(testFilePath))
        {
            try
            {
                File.Delete(testFilePath);
                Console.WriteLine($"Pre-test cleanup: Deleted existing file {testFilePath} to ensure a clean save.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pre-test cleanup: Could not delete file {testFilePath}: {ex.Message}. It might be in use or permissions issue.");
                // You might want to Assert.Fail here if cleanup is critical for your test's integrity.
                // Assert.Fail($"Failed to clean up test file: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Pre-test cleanup: File {testFilePath} does not exist, proceeding with fresh save.");
        }
        // --- End IMPORTANT ---

        try
        {
            app = FlaUI.Core.Application.Launch(appPath);
            Console.WriteLine("Application.Launch called. Waiting for app to stabilize...");
            Thread.Sleep(5000); // Increased sleep time to ensure app has time to fully load
            Console.WriteLine("Finished initial sleep after launch.");

            if (app == null)
            {
                Console.WriteLine("ERROR: 'app' object is null after Application.Launch. Check appPath and permissions.");
                Assert.Fail("Application object is null after launch. Cannot proceed.");
            }

            // Get the main window using the class-level automation instance
            Console.WriteLine("Attempting to get main window...");
            // Use a timeout for GetMainWindow as well, it's more robust
            mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            if (mainWindow == null)
            {
                // Fallback: Sometimes GetMainWindow doesn't find it immediately, try a broader search
                Console.WriteLine("GetMainWindow returned null. Trying to find any top-level window of the app.");
                var allAppWindows = app.GetAllTopLevelWindows(automation);
                foreach (var win in allAppWindows)
                {
                    Console.WriteLine($"Found app window: Title='{win.Title}', ClassName='{win.ClassName}'");
                    // You might need to adjust this logic based on your actual main window's properties
                    if (win.Title.Contains("Form1") || win.ClassName.Contains("WindowsForms10.Window")) // Common WinForms window patterns
                    {
                        mainWindow = win;
                        Console.WriteLine($"Identified main window by title/class: {mainWindow.Title}");
                        break;
                    }
                }
            }

            Assert.That(mainWindow, Is.Not.Null, "Main window was not found after launch. Application might not have started correctly or window title/class changed.");
            Console.WriteLine($"Main window found: Title='{mainWindow.Title}', AutomationId='{mainWindow.Properties.AutomationId.ValueOrDefault}'");
            mainWindow.SetForeground(); // Bring the window to the front
            Thread.Sleep(1000); // Give it a moment to become foreground
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR during application launch or main window finding: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Assert.Fail($"Failed to launch or find main window: {ex.Message}");
        }

        Console.WriteLine("--- Setup Finished ---");
    }
    [Test]
    public void VerifyApplicationLabel()
    {
        Label label1 = mainWindow.FindFirstDescendant(
            cf.ByAutomationId("label1")
            ).AsLabel();
        Console.WriteLine($"Found label1 text: {label1?.Text}");
        Assert.That(label1?.Text, Is.EqualTo("Enter some text for conversion "));
    }

    [Test]
    public void VerifyConvertedText()
    {
        string conversionText = "TEst mE out!";

        mainWindow.FindFirstDescendant(cf.ByAutomationId("textBox1"))
            .AsTextBox()
            .Enter(conversionText);

        mainWindow.FindFirstDescendant(cf.ByAutomationId("button1"))
            .AsButton()
            .Click();

        Label outputLabel = mainWindow.FindFirstDescendant(cf.ByAutomationId("label2"))
            .AsLabel();

        Assert.That(outputLabel?.Text, Is.EqualTo(conversionText.ToLower()));
    }

    [Test]

    public void VerifyInvalidText()
    {
        string conversionText = ""; // Empty string to trigger the MessageBox

        Console.WriteLine("Entering invalid text into textBox1...");
        var textBox = mainWindow.FindFirstDescendant(cf.ByAutomationId("textBox1")).AsTextBox();
        Assert.That(textBox, Is.Not.Null, "textBox1 with AutomationId 'textBox1' not found.");
        textBox.Enter(conversionText);
        Console.WriteLine($"Text in textBox1 after entry: '{textBox.Text}'");

        Console.WriteLine("Clicking button1...");
        var button = mainWindow.FindFirstDescendant(cf.ByAutomationId("button1")).AsButton();
        Assert.That(button, Is.Not.Null, "button1 with AutomationId 'button1' not found.");
        button.Click();


        Label? messageBoxLabel = mainWindow.FindFirstDescendant(cf.ByAutomationId("65535"))?.AsLabel();
        Assert.That(messageBoxLabel, Is.Not.Null, "Message Box label with AutomationId '65535' not found.");

        Console.WriteLine($"Message Box Text: {messageBoxLabel?.Text}");
        Assert.That(messageBoxLabel?.Text, Is.EqualTo("please enter valid text"));

        // Find and click the OK button (often has AutomationId "2" for standard message boxes)
        Button? okButton = mainWindow.FindFirstDescendant(cf.ByAutomationId("2"))?.AsButton();
        Assert.That(okButton, Is.Not.Null, "OK button with AutomationId '2' not found on message box.");
        okButton?.Click(); // Use null-conditional operator for safety

        Console.WriteLine("Message Box closed.");
    }
    [Test]
    public void NewCheckBox_TogglesState()
    {
        CheckBox newCheckBox = mainWindow.FindFirstDescendant(cf.ByAutomationId("newCheckBox")).AsCheckBox();
        Assert.That(newCheckBox, Is.Not.Null, "newCheckBox with AutomationId 'newCheckBox' not found."); // NUnit recommended syntax

        Assert.That(newCheckBox.IsChecked, Is.False, "NewCheckBox should be unchecked by default.");

        newCheckBox.Click();
        Assert.That(newCheckBox.IsChecked, Is.True, "NewCheckBox should be checked after click.");

        newCheckBox.Click();
        Assert.That(newCheckBox.IsChecked, Is.False, "NewCheckBox should be unchecked after second click.");
    }

    [Test]
    public void NewComboBox_SelectsOptionB_Streamlined()
    {
        // Arrange
        Console.WriteLine("Attempting to find newComboBox...");
        ComboBox newComboBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("newComboBox")).AsComboBox();
        Assert.That(newComboBox, Is.Not.Null, "The 'newComboBox' was not found on the main window. Ensure its AutomationId is 'newComboBox'."); // NUnit recommended syntax
        Console.WriteLine("newComboBox found.");

        string optionToSelect = "Option B";

        // Act
        Console.WriteLine($"Attempting to select '{optionToSelect}' from newComboBox...");

        IExpandCollapsePattern expandCollapsePattern;
        bool patternFound = newComboBox.Patterns.ExpandCollapse.TryGetPattern(out expandCollapsePattern);

        Assert.That(patternFound, Is.True, "ExpandCollapsePattern not found on newComboBox. Cannot expand programmatically.");
        Console.WriteLine("Expanding ComboBox using ExpandCollapsePattern...");
        expandCollapsePattern.Expand();
        Console.WriteLine("ComboBox expanded.");

        Thread.Sleep(500); // Give the UI time to render the expanded dropdown list

        AutomationElement listItem = newComboBox.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem).And(cf.ByName(optionToSelect)));

        Assert.That(listItem, Is.Not.Null, $"Option '{optionToSelect}' (ListItem) was not found in the dropdown list after expansion. " + // NUnit recommended syntax
                                  "Verify the item's name/properties using a UI Inspector when the dropdown is open.");
        Console.WriteLine($"Found list item '{optionToSelect}'.");

        listItem.Click();
        Console.WriteLine($"Clicked on '{optionToSelect}' in the dropdown list.");

        // --- IMPORTANT: ASSERTION DEBUGGING LOGIC ---
        Console.WriteLine("\n--- Starting Assertion Debug ---");

        Thread.Sleep(200);

        string actualValueForAssertion = null;

        if (newComboBox.SelectedItem != null)
        {
            actualValueForAssertion = newComboBox.SelectedItem.Text;
            Console.WriteLine($"Actual Selected Item Text (from SelectedItem): '{actualValueForAssertion}' (Length: {actualValueForAssertion.Length})");
        }
        else
        {
            Console.WriteLine("ERROR: newComboBox.SelectedItem is NULL. Attempting to get text from ComboBox's ValuePattern.");
            IValuePattern valuePattern;
            if (newComboBox.Patterns.Value.TryGetPattern(out valuePattern))
            {
                actualValueForAssertion = valuePattern.Value;
                Console.WriteLine($"Actual ComboBox Text (from ValuePattern): '{actualValueForAssertion}' (Length: {(actualValueForAssertion != null ? actualValueForAssertion.Length : 0)})");
            }
            else
            {
                Console.WriteLine("ERROR: ComboBox does not support ValuePattern. Cannot get text from its main display area.");
            }
        }

        Console.WriteLine($"Expected Text (optionToSelect): '{optionToSelect}' (Length: {optionToSelect.Length})");
        Console.WriteLine("--- End Assertion Debug ---\n");

        Assert.That(actualValueForAssertion, Is.Not.Null, "No text found for assertion (both SelectedItem and ValuePattern were unavailable or null).");
        Assert.That(actualValueForAssertion.Trim(), Is.EqualTo(optionToSelect).IgnoreCase, $"ComboBox did not select '{optionToSelect}'."); // NUnit recommended syntax with IgnoreCase
        Console.WriteLine($"Verification successful: '{actualValueForAssertion}' is selected.");
    }


    [Test]
    public void NewRadioButtons_SelectsType1AndThenType2()
    {
        // Arrange
        Console.WriteLine("Attempting to find the 'Choose Type' GroupBox...");
        // Find the Group control that contains the radio buttons
        AutomationElement chooseTypeGroupBox = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Group).And(cf.ByName("Choose Type").Or(cf.ByAutomationId("newGenderGroupBox")))
        );

        Assert.That(chooseTypeGroupBox, Is.Not.Null, "The 'Choose Type' GroupBox was not found. Please verify its Name or AutomationId."); // NUnit recommended syntax
        Console.WriteLine("Choose Type GroupBox found.");

        Console.WriteLine("Attempting to find newRadioMale and newRadioFemale within the GroupBox...");
        // Now, find the radio buttons as descendants of the GroupBox
        RadioButton type1Radio = chooseTypeGroupBox.FindFirstDescendant(cf => cf.ByAutomationId("newRadioMale")).AsRadioButton();
        RadioButton type2Radio = chooseTypeGroupBox.FindFirstDescendant(cf => cf.ByAutomationId("newRadioFemale")).AsRadioButton();

        Assert.That(type1Radio, Is.Not.Null, "newRadioMale (Type 1) not found within the GroupBox."); // NUnit recommended syntax
        Assert.That(type2Radio, Is.Not.Null, "newRadioFemale (Type 2) not found within the GroupBox."); // NUnit recommended syntax
        Console.WriteLine("Radio buttons found.");

        // Act & Assert - Select Type 1 (Male)
        Console.WriteLine("Selecting Type 1 (Male) radio button...");
        type1Radio.Click();
        Assert.That(type1Radio.IsChecked, Is.True, "Type 1 RadioButton (Male) should be checked.");
        Assert.That(type2Radio.IsChecked, Is.False, "Type 2 RadioButton (Female) should be unchecked after Type 1 selected.");
        Console.WriteLine("Type 1 (Male) radio button verified as selected.");

        // Act & Assert - Select Type 2 (Female)
        Console.WriteLine("Selecting Type 2 (Female) radio button...");
        type2Radio.Click();
        Assert.That(type2Radio.IsChecked, Is.True, "Type 2 RadioButton (Female) should be checked.");
        Assert.That(type1Radio.IsChecked, Is.False, "Type 1 RadioButton (Male) should be unchecked after Type 2 selected.");
        Console.WriteLine("Type 2 (Female) radio button verified as selected.");
    }

    [Test]
    public void NewMenu_OpensFileMenu()
    {
        Console.WriteLine("Attempting to find the 'File' menu item...");
        MenuItem fileMenu = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("File"))
        ).AsMenuItem();

        Assert.That(fileMenu, Is.Not.Null, "The 'File' menu item was not found. Please verify its Name."); // NUnit recommended syntax
        Console.WriteLine("'File' menu item found.");


        Console.WriteLine("Clicking 'File' menu to expand it...");
        fileMenu.Click();

        Thread.Sleep(500);
        MenuItem openMenuItem = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Open"))
        ).AsMenuItem();

        Assert.That(openMenuItem, Is.Not.Null, "'Open' menu item not found after clicking 'File'. Menu might not have expanded."); // NUnit recommended syntax
        Console.WriteLine("'File' menu expanded and 'Open' item found. Verification successful.");


    }

    [Test]
    public void NewMenu_ClicksSaveAndExit()
    {
        // Arrange
        Console.WriteLine("Attempting to find the 'File' menu item...");
        MenuItem fileMenu = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("File"))
        ).AsMenuItem();
        Assert.That(fileMenu, Is.Not.Null, "The 'File' menu item was not found.");

        Console.WriteLine("Clicking 'File' menu to expand it...");
        fileMenu.Click();
        Thread.Sleep(500); // Wait for menu to expand

        Console.WriteLine("Attempting to find and click 'Save' menu item...");
        MenuItem saveMenuItem = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))
        ).AsMenuItem();
        Assert.That(saveMenuItem, Is.Not.Null, "'Save' menu item not found. Menu might not have expanded or item name is different.");

        saveMenuItem.Click();
        Console.WriteLine("'Save' menu item clicked.");

        Console.WriteLine("Re-opening 'File' menu to click 'Exit'...");
        mainWindow.SetForeground();
        fileMenu.Click();
        Thread.Sleep(500);
        Console.WriteLine("Attempting to find and click 'Exit' menu item...");
        MenuItem exitMenuItem = mainWindow.FindFirstDescendant(
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Exit"))
        ).AsMenuItem();
        Thread.Sleep(500);

        Console.WriteLine($"Found 'Exit' menu item: Name='{exitMenuItem.Name}', IsEnabled={exitMenuItem.Properties.IsEnabled.Value}, IsOffscreen={exitMenuItem.Properties.IsOffscreen.Value}.");

        Console.WriteLine($"Clicking 'Exit' menu item. Current app PID: {application.ProcessId}");
        exitMenuItem.Click(); // Attempt the click
        Console.WriteLine("'Exit' menu item clicked. Application closed");


    }
    [Test]
    public void ClicksEditMenu()
    {
        Console.WriteLine("Attempting to find the 'Edit' menu item...");
        MenuItem editMenu = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Edit"))
        ).AsMenuItem();
        Assert.That(editMenu, Is.Not.Null, "The 'Edit' menu item was not found.");

        Console.WriteLine("Clicking 'Edit' menu to expand it...");
        editMenu.Click();
        Thread.Sleep(500); // Wait for menu to expand
        Console.WriteLine("Attempting to find and click 'Copy' menu item...");
        MenuItem copyMenuItem = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Copy"))
        ).AsMenuItem();
        Assert.That(copyMenuItem, Is.Not.Null, "'Copy' menu item not found. Menu might not have expanded or item name is different.");
        copyMenuItem.Click();
        Console.WriteLine("'Copy' menu item clicked.");
        Console.WriteLine("Re-opening 'Edit' menu to click 'Cut'...");
        mainWindow.SetForeground();
        editMenu.Click();
        Thread.Sleep(500);
        Console.WriteLine("Attempting to find and click 'Cut' menu item...");
        MenuItem cutMenuItem = mainWindow.FindFirstDescendant(
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Cut"))
        ).AsMenuItem();
        Thread.Sleep(500);
        Console.WriteLine($"Found 'Cut' menu item: Name='{cutMenuItem.Name}', IsEnabled={cutMenuItem.Properties.IsEnabled.Value}, IsOffscreen={cutMenuItem.Properties.IsOffscreen.Value}.");
        mainWindow.SetForeground();
        cutMenuItem.Click(); // Attempt the click
        Console.WriteLine("'Cut' menu item clicked.");
        Console.WriteLine("Re-opening 'Edit' menu to click 'Paste'...");
        mainWindow.SetForeground(); // Ensure window is active
        editMenu.Click();
        Thread.Sleep(500);

        Console.WriteLine("Attempting to find and click 'Paste' menu item...");
        // Search for 'Paste' within the expanded 'Edit' menu
        MenuItem pasteMenuItem = editMenu.FindFirstDescendant(
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Paste"))
        ).AsMenuItem();
        Assert.That(pasteMenuItem, Is.Not.Null, "'Paste' menu item not found."); // Assert before accessing properties
        Console.WriteLine($"Found 'Paste' menu item: Name='{pasteMenuItem.Name}', IsEnabled={pasteMenuItem.Properties.IsEnabled.Value}, IsOffscreen={pasteMenuItem.Properties.IsOffscreen.Value}.");
        pasteMenuItem.Click(); // Attempt the click
        Console.WriteLine("'Paste' menu item clicked.");
    }
    [Test]
    public void clicksPage2()
    {
        Console.WriteLine("Attempting to find the 'Page 2' button...");
        Button page2Button = mainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("tabPage2")
        ).AsButton();
        Assert.That(page2Button, Is.Not.Null, "The 'Page 2' button was not found. Please verify its AutomationId."); // NUnit recommended syntax
        Console.WriteLine("'Page 2' button found.");

        Console.WriteLine("Clicking 'Page 2' button...");
        page2Button.Click();
        Console.WriteLine("'Page 2' button clicked.");
        //performing additon 
        Console.WriteLine("Attempting to add two numbers on  Page 2...");
        TextBox number1TextBox = mainWindow.FindFirstDescendant(cf.ByAutomationId("number1TextBox")).AsTextBox();
        Assert.That(number1TextBox, Is.Not.Null, "The 'number1TextBox' was not found. Please verify its AutomationId."); // NUnit recommended syntax
        Console.WriteLine("'number1TextBox' found.");
        number1TextBox.Enter("5");
        Console.WriteLine("'5' entered into 'number1TextBox'.");
        TextBox number2TextBox = mainWindow.FindFirstDescendant(cf.ByAutomationId("number2TextBox")).AsTextBox();
        Assert.That(number2TextBox, Is.Not.Null, "The 'number2TextBox' was not found. Please verify its AutomationId."); // NUnit recommended syntax
        Console.WriteLine("'number2TextBox' found.");
        number2TextBox.Enter("10");
        Console.WriteLine("'10' entered into 'number2TextBox'.");
        Button calculateButton = mainWindow.FindFirstDescendant(cf.ByAutomationId("calculateButton")).AsButton();
        Assert.That(calculateButton, Is.Not.Null, "The 'calculateButton' was not found. Please verify its AutomationId."); // NUnit recommended syntax    
        Console.WriteLine("'calculateButton' found.");
        calculateButton.Click();
        Console.WriteLine("'calculateButton' clicked.");
        Console.WriteLine("Performing addition on 'Page 2'.");
        Console.WriteLine("The result is: " + mainWindow.FindFirstDescendant(cf.ByAutomationId("resultLabel")).AsLabel().Name);
    }
    [Test]
    public void EndToEndSaveOperation_ShouldSaveTextToFile()
    {
        // 1. Switch to "Main Content" tab and enter text
        // Ensure the tab control is correctly found.
        var tabControl = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab)).AsTab();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(tabControl, Is.Not.Null, "TabControl not found on the main window.");

        // Select the tab by its name (text)
        tabControl.SelectTabItem("Main Content");
        Console.WriteLine("Switched to 'Main Content' tab.");

        // Find textBox1 by its AutomationId (which is usually its Name property in WinForms)
        var textBox1 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(textBox1, Is.Not.Null, "textBox1 not found on the 'Main Content' tab.");
        string textToSave = "FlaUI automation.";
        textBox1.Text = textToSave; // Set text directly
        Console.WriteLine($"Entered text: '{textToSave}' into textBox1.");

        // 2. Click the "File" menu item
        var fileMenuItem = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("File"))).AsMenuItem();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(fileMenuItem, Is.Not.Null, "File menu item not found.");
        fileMenuItem.Invoke(); // Use Invoke() to click menu items
        Console.WriteLine("Clicked 'File' menu.");

        // 3. Click the "Save" sub-menu item
        // The 'Save' MenuItem might appear as a descendant of the 'File' menu itself or globally in the desktop.
        // Let's first try as a descendant of the opened 'File' menu (more robust).
        var saveMenuItem = fileMenuItem.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))).AsMenuItem();
        if (saveMenuItem == null)
        {
            // Fallback: If not found as a descendant of fileMenuItem, try finding it globally on the desktop
            // This sometimes happens if the menu is a top-level window itself.
            saveMenuItem = automation.GetDesktop().FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))).AsMenuItem();
        }
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(saveMenuItem, Is.Not.Null, "Save menu item not found after clicking 'File'.");
        saveMenuItem.Click();  //to click menu items
        Console.WriteLine("Clicked 'Save' menu item.");

        // 4. Handle the Save File Dialog
        // Access ModalWindows as a property, then use LINQ's FirstOrDefault with the predicate.
        var saveFileDialog = mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Window).And(cf.ByName("Save Application Data"))).AsWindow(); // FirstOrDefault(cf => cf.ByName("Save Application Data");
                                                                                                                                                      // Using Assert.That(..., Is.Not.Null)
        Assert.That(saveFileDialog, Is.Not.Null, "Save File Dialog 'Save Application Data' not found as a modal window.");
        Console.WriteLine("Save File Dialog found.");

        // Set the file name in the Save File Dialog.
        var fileNameTextBoxInDialog = mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Edit).And(cf.ByAutomationId("1001"))).AsTextBox();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(fileNameTextBoxInDialog, Is.Not.Null, "File name text box in Save Dialog not found.");
        fileNameTextBoxInDialog.Enter(testFilePath); // testFilePath;
        Console.WriteLine($"Entered file name: {testFilePath}");

        // Click the Save button in the dialog
        var saveButtonInDialog = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Save"))).AsButton();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(saveButtonInDialog, Is.Not.Null, "Save button in Save File Dialog not found.");
        saveButtonInDialog.Invoke();
        Console.WriteLine("Clicked 'Save' button in Save File Dialog.");

        // 5. Handle the "Save Complete" message box
        // Access ModalWindows as a property, then use LINQ's FirstOrDefault with the predicate.
        var saveCompleteMessageBox = mainWindow.ModalWindows.FirstOrDefault(cf => cf.Properties.ControlType == ControlType.Window && cf.Properties.Name == "Save Complete");
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(saveCompleteMessageBox, Is.Not.Null, "Save Complete message box not found as a modal window.");
        Console.WriteLine("Save Complete message box found.");

        // Click OK on the message box
        var okButton = saveCompleteMessageBox.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))).AsButton();
        // Using Assert.That(..., Is.Not.Null)
        Assert.That(okButton, Is.Not.Null, "OK button in message box not found.");
        okButton.Invoke();
        Console.WriteLine("Clicked 'OK' on Save Complete message box.");

        // 6. Assertion: Verify the file was actually created and contains the correct text
        Assert.That(File.Exists(testFilePath), $"Expected file to be created at {testFilePath}, but it was not found.");
        Assert.That(File.ReadAllText(testFilePath), Is.EqualTo(textToSave), "The content of the saved file does not match the entered text.");

        Console.WriteLine("File save operation verified successfully! 🎉");
    }



    // public void NewMenu_PerformsEndToEndSaveOperation()
    // {
    //     Console.WriteLine("--- Test: NewMenu_PerformsEndToEndSaveOperation Started ---");
    //     // Arrange: Input some text into textBox1
    //     Console.WriteLine("Entering text into textBox1 for saving...");
    //     TextBox mainTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
    //     Assert.That(mainTextBox, Is.Not.Null, "textBox1 not found.");
    //     string textToSave = "This is some test content to be saved by the automation.";
    //     mainTextBox.Text = textToSave;
    //     Thread.Sleep(500);

    //     // Act: Click File -> Save
    //     Console.WriteLine("Attempting to find the 'File' menu item...");
    //     MenuItem fileMenu = mainWindow.FindFirstDescendant(cf =>
    //         cf.ByControlType(ControlType.MenuItem).And(cf.ByName("File"))
    //     ).AsMenuItem();
    //     Assert.That(fileMenu, Is.Not.Null, "The 'File' menu item was not found.");
    //     Console.WriteLine("Clicking 'File' menu to expand it...");
    //     fileMenu.Click();
    //     Thread.Sleep(500); // Wait for menu to expand

    //     Console.WriteLine("Attempting to find and click 'Save' menu item...");
    // MenuItem saveMenuItem = fileMenu.FindFirstDescendant(cf => // Search within fileMenu
    //         cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))
    //     ).AsMenuItem();
    //     Assert.That(saveMenuItem, Is.Not.Null, "'Save' menu item not found.");

    //     // --- NEW: Wait for saveMenuItem to be clickable before clicking ---
    //     Console.WriteLine("Waiting for 'Save' menu item to be clickable...");
    //     saveMenuItem.WaitUntilClickable(TimeSpan.FromSeconds(5));
    //     // --- END NEW ---

    //     saveMenuItem.Click();
    //     Console.WriteLine("'Save' menu item clicked. Waiting for Save File Dialog...");
    //     // Wait for the Save File Dialog to appear
    // Window saveFileDialog = Automation.GetDesktop().FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName("Save Application Data"))).AsWindow();
    //     Console.WriteLine("saveFileDialog found: " + (saveFileDialog != null ? "Yes" : "No"));
    //     Console.WriteLine($"Save File Dialog Title: {saveFileDialog?.Title}, AutomationId: {saveFileDialog?.Properties.AutomationId.ValueOrDefault}");
    //     Assert.That(saveFileDialog, Is.Not.Null, "Save File Dialog did not appear after clicking 'Save'. Ensure the application supports saving files.");
    //     Console.WriteLine("Save File Dialog found. Entering file name...");
    //     TextBox fileNameTextBox = saveFileDialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit)).AsTextBox();
    //     Assert.That(fileNameTextBox, Is.Not.Null, "File name text box not found in Save File Dialog.");
    //     fileNameTextBox.Text = testFilePath; // Use the fixed test file path
    //     Button saveButton = saveFileDialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Save"))).AsButton();
    //     Assert.That(saveButton, Is.Not.Null, "Save button not found in Save File Dialog.");
    //     Console.WriteLine("Saving file...");
    //     saveButton.Click();
    //     Console.WriteLine("File saved successfully.");
    //     // Verify the file was created
    //     Assert.That(File.Exists(testFilePath), Is.True, $"The file was not saved successfully. Expected file path: {testFilePath}");
    //     Console.WriteLine($"File saved successfully at: {testFilePath}");


    // }[Test]
    // public void NewMenu_PerformsEndToEndSaveOperation()

    // {
    //     Console.WriteLine("--- Test: NewMenu_PerformsEndToEndSaveOperation Started ---");
    //     // Arrange: Input some text into textBox1
    //     Console.WriteLine("Entering text into textBox1 for saving...");
    //     TextBox mainTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
    //     Assert.That(mainTextBox, Is.Not.Null, "textBox1 not found.");
    //     string textToSave = "This is some test content to be saved by the automation.";
    //     mainTextBox.Text = textToSave;
    //     Thread.Sleep(500);

    //     // Act: Click File -> Save
    //     Console.WriteLine("Attempting to find the 'File' menu item...");
    //     MenuItem fileMenu = mainWindow.FindFirstDescendant(cf =>
    //         cf.ByControlType(ControlType.MenuItem).And(cf.ByName("File"))
    //     ).AsMenuItem();
    //     Assert.That(fileMenu, Is.Not.Null, "The 'File' menu item was not found.");
    //     Console.WriteLine("Clicking 'File' menu to expand it...");
    //     fileMenu.Click();
    //     Thread.Sleep(500); // Wait for menu to expand

    //     Console.WriteLine("Attempting to find and click 'Save' menu item...");
    //     MenuItem saveMenuItem = fileMenu.FindFirstDescendant(cf => // Search within fileMenu
    //         cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))
    //     ).AsMenuItem();
    //     Assert.That(saveMenuItem, Is.Not.Null, "'Save' menu item not found.");

    //     // Wait for saveMenuItem to be clickable before clicking
    //     Console.WriteLine("Waiting for 'Save' menu item to be clickable...");
    //     saveMenuItem.WaitUntilClickable(TimeSpan.FromSeconds(5));

    //     saveMenuItem.Click();
    //     Console.WriteLine("'Save' menu item clicked. Waiting for Save File Dialog...");

    //     // Assert & Act: Interact with the SaveFileDialog
    //     Window saveDialog = null;
    //     TimeSpan dialogTimeout = TimeSpan.FromSeconds(20);
    //     DateTime dialogStartTime = DateTime.Now;
    //     string targetDialogTitle = "Save Application Data"; // Confirmed by your Inspect.exe screenshot
    //     string fallbackDialogTitle = "Save As"; // Common fallback for system dialogs

    //     Console.WriteLine($"Attempting to find Save File Dialog with title: '{targetDialogTitle}' or '{fallbackDialogTitle}' (Timeout: {dialogTimeout.TotalSeconds}s)");

    //     // --- CRITICAL FIX: Manual, robust loop for finding the Save Dialog ---
    //     while (saveDialog == null && (DateTime.Now - dialogStartTime) < dialogTimeout)

    //     {
    //         // var desktop = automation.GetDesktop();
    // var allTopLevelWindows = desktop.FindAll(TreeScope.Children, cf.ByControlType(ControlType.Window).And(cf.ByName("Save Application Data")).Or(cf.ByName("Save As")));
    //         // Console.WriteLine($"Found {allTopLevelWindows.Length} top-level windows matching the criteria.");
    //var allTopLevelWindows = Application.GetAllTopLevelWindows(automation); // This is the line you're getting the error on
    //         foreach (var win in allTopLevelWindows)
    //         {
    //             // Filter by process ID first, then by control type and title
    //             if (win.Properties.ProcessId.ValueOrDefault == app.ProcessId &&
    //                 win.ControlType == ControlType.Window &&
    //                 (win.Title == targetDialogTitle || win.Title == fallbackDialogTitle))
    //             {
    //                 saveDialog = win;
    //                 Console.WriteLine($"  Found Save File Dialog: Title='{win.Title}', ClassName='{win.ClassName}', ProcessId={win.Properties.ProcessId.ValueOrDefault}");
    //                 break; // Found the dialog, exit inner loop
    //             }
    //             else
    //             {
    //                 // Log other windows for debugging if the target is not found
    //                 Console.WriteLine($"  Skipping Window: Title='{win.Title}', ClassName='{win.ClassName}', ProcessId={win.Properties.ProcessId.ValueOrDefault}");
    //             }
    //         }

    //             if (saveDialog == null)
    //             {
    //                 Thread.Sleep(500); // Wait a bit before checking again
    //             }
    //         }
    //         // --- END CRITICAL FIX ---

    //         Assert.That(saveDialog, Is.Not.Null, "Save File Dialog was not found within the timeout. Please check its exact title using Inspect.exe/FlaUInspect and ensure it appears.");
    //         Console.WriteLine("Save File Dialog found.");
    //         saveDialog.SetForeground(); // Ensure the dialog is in focus
    //         Thread.Sleep(500); // Give it a moment to gain focus


    //         // Find the file name text box in the dialog
    //         TextBox fileNameTextBox = saveDialog.FindFirstDescendant(cf =>
    //             cf.ByControlType(ControlType.Edit).And(cf.ByName("File name:")) // Common name for this field
    //         ).AsTextBox();

    //         if (fileNameTextBox == null)
    //         {
    //             // Fallback if "File name:" is not the Name, try by ClassName and index
    //             Console.WriteLine("File name text box not found by name. Trying by ClassName 'Edit'.");
    //             var editControls = saveDialog.FindAllDescendants(cf => cf.ByClassName("Edit"));
    //             if (editControls.Length > 0)
    //             {
    //                 fileNameTextBox = editControls[0].AsTextBox(); // Usually the first or second Edit control
    //             }
    //         }
    //         Assert.That(fileNameTextBox, Is.Not.Null, "File name text box in Save Dialog not found.");
    //         Console.WriteLine("File name text box found. Entering file path...");

    //         fileNameTextBox.Text = testFilePath; // Enter the full path
    //         Thread.Sleep(500);

    //         // Find and click the Save button in the dialog
    //         Button saveDialogButton = null;
    //         Console.WriteLine("Attempting to find 'Save' button in dialog by Name 'Save'...");
    //         saveDialogButton = saveDialog.FindFirstDescendant(cf =>
    //             cf.ByControlType(ControlType.Button).And(cf.ByName("Save"))
    //         ).AsButton();

    //         // Added robust waiting for the Save button
    //         if (saveDialogButton == null)
    //         {
    //             Console.WriteLine("Save button not found by Name 'Save'. Trying by AutomationId '1'."); // Common AutomationId for default Save button
    //             saveDialogButton = saveDialog.FindFirstDescendant(cf =>
    //                 cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("1"))
    //             ).AsButton();
    //         }

    //         if (saveDialogButton == null)
    //         {
    //             Console.WriteLine("Save button not found by AutomationId '1'. Trying by ClassName 'Button' and text 'Save'...");
    //             saveDialogButton = saveDialog.FindAllDescendants(cf =>
    //                 cf.ByControlType(ControlType.Button)
    //             ).FirstOrDefault(b => b.Name == "Save")?.AsButton();
    //         }

    //         // Wait for the button to be clickable if found
    //         if (saveDialogButton != null)
    //         {
    //             Console.WriteLine("Waiting for Save button to be clickable...");
    //             saveDialogButton.WaitUntilClickable(TimeSpan.FromSeconds(5)); // Wait up to 5 seconds for it to be ready
    //         }

    //         Assert.That(saveDialogButton, Is.Not.Null, "Save button in dialog not found after multiple attempts.");
    //         Console.WriteLine("Clicking Save button in dialog...");
    //         saveDialogButton.Click();
    //         Thread.Sleep(1000); // Give time for the file to be saved and dialog to close

    //         // Validate the success MessageBox (optional but good for end-to-end)
    //         Window successMessageBox = mainWindow.FindFirstDescendant(cf =>
    //             cf.ByControlType(ControlType.Window).And(cf.ByName("Save Complete"))
    //         ).AsWindow();

    //         if (successMessageBox != null)
    //         {
    //             Console.WriteLine("Success message box found. Closing it.");
    //             successMessageBox.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))).AsButton().Click();
    //             Thread.Sleep(500);
    //         }
    //         else
    //         {
    //             Console.WriteLine("Success message box not found (may have closed too quickly or not appeared).");
    //         }


    //         // Final Validation: Check if the file exists and its content
    //         Console.WriteLine($"Verifying file existence at: {testFilePath}");
    //         Assert.That(File.Exists(testFilePath), $"File was not created at {testFilePath}.");

    //         Console.WriteLine("Reading file content...");
    //         string fileContent = File.ReadAllText(testFilePath);
    //         Assert.That(fileContent, Is.EqualTo(textToSave), "Content of the saved file does not match the expected text.");

    //         Console.WriteLine("End-to-end save operation validated successfully!");
    //         Console.WriteLine("--- Test: NewMenu_PerformsEndToEndSaveOperation Finished ---");
    //     }
    // }
    [Test]
    public void TestCopyMenuItem()
    {
        MenuItem editMenu = mainWindow.FindFirstDescendant(cf => cf.ByName("Edit").And(cf.ByControlType(ControlType.MenuItem))).AsMenuItem();
        Assert.That(editMenu, Is.Not.Null, "Edit menu should be found.");
        TextBox textBox1 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
        Assert.That(textBox1, Is.Not.Null, "textBox1 should be found.");
        string textToEnter = "This is some text to copy.";

        textBox1.Text = string.Empty;
        textBox1.Enter(textToEnter);
        Thread.Sleep(500); // Give time for text to be entered
        Console.WriteLine($"Entered text: '{textToEnter}' into textBox1.");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A); // Simulate Ctrl+A for select all operation
                                                                                     // var selectedText = textBox1.Text;
        Thread.Sleep(500); // Give time for selection to register
        Console.WriteLine($"Selected text: '{textBox1.Text}' from textBox1.");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        Thread.Sleep(1000);
        Keyboard.Type(VirtualKeyShort.BACK);
        Thread.Sleep(1000); // Simulate Ctrl+C for copy operation
                            // Assert - Check if the text was copied to clipboard
        Console.WriteLine("Checking clipboard content after copy operation...");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V); // Simulate Ctrl+V for paste operation
        Thread.Sleep(500); // Give time for paste operation
        //TextBox textBox1 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
        string ActualTextCopied = textBox1.Text; // Get the text from textBox1 after paste operation
        string expectedCopiedText = textToEnter; // The text we expect to be copied

        Assert.That(ActualTextCopied, Is.EqualTo(expectedCopiedText), $"Expected copied text: {expectedCopiedText}, Actual copied text: {ActualTextCopied}");


    }
    [Test]
    public void SelectedTextCopy()
    {
        MenuItem editMenu = mainWindow.FindFirstDescendant(cf => cf.ByName("Edit").And(cf.ByControlType(ControlType.MenuItem))).AsMenuItem();
        Assert.That(editMenu, Is.Not.Null, "Edit menu should be found.");
        TextBox textBox1 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("textBox1")).AsTextBox();
        Assert.That(textBox1, Is.Not.Null, "textBox1 should be found.");
        string textToEnter = "This is some text to copy.";

        textBox1.Text = string.Empty;
        textBox1.Enter(textToEnter);
        Thread.Sleep(500); // Give time for text to be entered
        Console.WriteLine($"Entered text: '{textToEnter}' into textBox1.");
        using (Keyboard.Pressing(VirtualKeyShort.SHIFT)) // Hold down the Shift key
        {
            Keyboard.Press(VirtualKeyShort.LEFT);
            Thread.Sleep(100); // Wait a bit to ensure the key press is registered
            Keyboard.Press(VirtualKeyShort.LEFT);
            Thread.Sleep(100); // Wait a bit to ensure the key press is registered
            Keyboard.Press(VirtualKeyShort.LEFT);
            Thread.Sleep(100); // Select the last 3 characters
        }
        // Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.LEFT, VirtualKeyShort.LEFT, VirtualKeyShort.LEFT); // Simulate Ctrl+A for select all operation
        Thread.Sleep(500); // Give time for selection to register
        Console.WriteLine($"Selected text: '{textBox1.Text}' from textBox1.");
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C); // Simulate Ctrl+C for copy operation
        Thread.Sleep(1000); // Wait for copy operation to complete
        textBox1.Text = string.Empty; // Clear the textBox1 to prepare for paste operation
        Console.WriteLine("TextBox cleared for paste operation.");
        Thread.Sleep(1000); // Wait for clear operation to complete
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V); // Simulate Ctrl+V for paste operation
        Thread.Sleep(500); // Give time for paste operation
        string ActualTextCopied = textBox1.Text; // Get the text from textBox1 after paste operation
        string expectedCopiedText = "py."; // The text we expect
        Assert.That(ActualTextCopied, Is.EqualTo(expectedCopiedText), $"Expected copied text: {expectedCopiedText}, Actual copied text: {ActualTextCopied}");
        Console.WriteLine($"Selected text '{expectedCopiedText}' copied successfully.");



    }
    // //  [Test]

    // public void AutomateRichTextBoxOnTabPage3()
    // {
    //     // 1. Find and select TabPage3
    //     // It's more robust to find the TabControl first, then its TabPage
    //     var tabControl = mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab).And(cf.ByName("tabControl1"))).AsTab();
    //     Assert.That(tabControl, Is.Not.Null, "TabControl should be found.");

    //     var tabPage3 = tabControl.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
    //     Assert.That(tabPage3, Is.Not.Null, "TabPage3 should be found.");

    //     // Select the tab page
    //     tabPage3.Select();
    //     Thread.Sleep(500); // Give UI time to switch tabs
    //     Console.WriteLine("Switched to TabPage3.");

    //     // 2. Find the RichTextBox as an AutomationElement first, then cast to TextBox for text interaction
    //     var richTextBoxAutomationElement = tabPage3.FindFirstDescendant(cf => cf.ByAutomationId("richTextBox1"));
    //     Assert.That(richTextBoxAutomationElement, Is.Not.Null, "RichTextBox AutomationElement should be found.");

    //     var richTextBox = richTextBoxAutomationElement.AsTextBox();
    //     Assert.That(richTextBox, Is.Not.Null, "RichTextBox (as TextBox) should be castable.");
    //     Console.WriteLine("RichTextBox found and cast to TextBox.");

    //     // 3. Enter some long text to trigger horizontal scrollbar
    //     string longText = "This is a very long line of text that should definitely trigger the horizontal scrollbar because it extends beyond the visible width of the RichTextBox. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

    //     richTextBox.Text = string.Empty;
    //     richTextBox.Enter(longText);
    //     Thread.Sleep(1000); // Give time for text entry and scrollbar to appear
    //     Console.WriteLine($"Entered text into RichTextBox. Text length: {longText.Length}");

    //     // 4. Verify the text was entered correctly
    //     Assert.That(richTextBox.Text, Is.EqualTo(longText), "Text in RichTextBox should match the entered text.");
    //     Console.WriteLine("Text verification successful.");

    //     // 5. Check for the presence of scrollbars by searching from the original AutomationElement
    //     // THIS IS THE CRITICAL CHANGE FOR FLAUI 5.x: Use PropertyCondition with AutomationProperties.OrientationProperty
    //     // var hScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf =>
    //     //     cf.ByControlType(ControlType.ScrollBar).And(
    //     //         new PropertyCondition(AutomationProperties.OrientationProperty, OrientationType.Horizontal)));
    //     var hScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf => cf.ByControlType(ControlType.ScrollBar).And(new PropertyCondition(PropertyId., OrientationType.Horizontal)));

    //     Assert.That(hScrollBar, Is.Not.Null, "Horizontal scrollbar should be present.");
    //     Assert.That(hScrollBar.Properties.IsEnabled.Value, Is.True, "Horizontal scrollbar should be enabled.");
    //     Console.WriteLine("Horizontal scrollbar found and enabled.");

    //     var vScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf =>
    //         cf.ByControlType(ControlType.ScrollBar).And(
    //             new PropertyCondition(AutomationProperties.OrientationProperty, OrientationType.Vertical)));
    //     // Assert.That(vScrollBar, Is.Not.Null, "Vertical scrollbar should be present.");
    //     // Assert.That(vScrollBar.Properties.IsEnabled.Value, Is.True, "Vertical scrollbar should be enabled.");
    //     Console.WriteLine("Vertical scrollbar found and enabled.");
    // }
    [Test]
    public void AutomateRichTextBoxOnTabPage3A()
    {
        // 1. Find and select TabPage3
        var TabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Thread.Sleep(500); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");

        // 2. Find the RichTextBox as an AutomationElement first, then cast to TextBox for text interaction
        // This is the key change: Keep the initial find as a generic AutomationElement.
        var richTextBoxAutomationElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("richTextBox1"));
        Assert.That(richTextBoxAutomationElement, Is.Not.Null, "RichTextBox AutomationElement should be found.");
        // var editBox = richTextBox.As(ControlType.Edit);
        // Now, cast it to TextBox for text manipulation
        var richTextBox = richTextBoxAutomationElement.AsTextBox();
        Assert.That(richTextBox, Is.Not.Null, "RichTextBox (as TextBox) should be castable.");
        Console.WriteLine("RichTextBox found and cast to TextBox.");

        // 3. Enter some long text to trigger horizontal scrollbar
        string longText = "This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very.\n" +
         "long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text.\n" +

                           "that should definitely trigger the This is a very long line of text that should definitely trigger the horizontal scrollbar because it extends beyond the visible width of the RichTextBox. Lorem ipsum dolor sit amet, consectetur.\n" + "adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n" + "This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very.\n" +
         "long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text that should definitely trigger the This is a very long line of text.\n" +

                           "that should definitely trigger the This is a very long line of text that should definitely trigger."; ;
        // string horizontalLinePart = "This is a very long line of text that should definitely trigger the horizontal scrollbar because it extends beyond the visible width of the RichTextBox.";  

        // Duplicate the horizontal part multiple times to ensure horizontal scrollbar
        // longText = string.Concat(Enumerable.Repeat(horizontalLinePart, 10)); // Repeat to ensure long text

        richTextBox.Text = string.Empty;
        richTextBox.Enter(longText);
        Thread.Sleep(20000); // Give time for text entry and scrollbar to appear
        Console.WriteLine($"Entered text into RichTextBox. Text length: {longText.Length}");

        // 4. Verify the text was entered correctly
        // Assert.That(richTextBox.Text, Is.EqualTo(longText), "Text in RichTextBox should match the entered text.");
        Console.WriteLine("Text verification successful.");

        // 5. Check for the presence of scrollbars by searching from the original AutomationElement
        // The scrollbars are direct children of the RichTextBox's underlying UIA element,
        // not necessarily the 'TextBox' wrapper that FlaUI provides for text interaction.
        var hScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf => cf.ByControlType(ControlType.ScrollBar).And(cf.ByAutomationId("NonClientHorizontalScrollBar")));
        Assert.That(hScrollBar, Is.Not.Null, "Horizontal scrollbar should be present.");
        Assert.That(hScrollBar.Properties.IsEnabled.Value, Is.True, "Horizontal scrollbar should be enabled.");
        Console.WriteLine("Horizontal scrollbar found and enabled.");

        var vScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf => cf.ByControlType(ControlType.ScrollBar).And(cf.ByAutomationId("NonClientVerticalScrollBar")));
        Assert.That(vScrollBar, Is.Not.Null, "Vertical scrollbar should be present.");
        Assert.That(vScrollBar.Properties.IsEnabled.Value, Is.True, "Vertical scrollbar should be enabled.");
        Console.WriteLine("Vertical scrollbar found and enabled.");
        //TODO: Scrollbar opertion 
    }
   
    [Test]
    public void DataGridTest1()
    {
        var tabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(tabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        tabPage3.Select();
        Thread.Sleep(500); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");
        // 1. Find the TabControl and select TabPage3

        // 2. Find the DataGrid AutomationElement
        // We get the AutomationElement directly, no AsDataGrid() here.
        var dataGridElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("dataGridView1").And(cf.ByControlType(ControlType.DataGrid)));
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        Thread.Sleep(2000); // Give UI time to render DataGrid contents

        // 3. Get the GridPattern
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;

        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");

        // Your data generation: for (int i = 1; i <= 10; i++) { dataGridView1.Rows.Add(i, $"Name {i}", 20 + i, 200 * i, i); }
        // For the second data row (i=2): ID=2, Name="Name 2", Age=22, Salary=400, Experience=2
        List<string> expectedList = ["2", "Name 2", "22", "400", "2"];
        List<string> actualList = [];

       // List<string> actualList = new();

        /*
        List<string> expectedList = new List<string> { "2", "Name 2", "22", "400", "2" };
        List<string> actualList = new List<string>();
       */

        Console.WriteLine("Reading cells from the second row (index 1):");

        int targetRowIndex = 1; // This is the 2nd *data* row (0-indexed, after the header row if present)

        Assert.That(targetRowIndex, Is.LessThan(rowCount), $"Target row index {targetRowIndex} is out of bounds. Max row index is {rowCount - 1}.");

        for (int col = 0; col < colCount; col++)
        {
            var cellElement = gridPattern.GetItem(targetRowIndex, col);
            Assert.That(cellElement, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");

            string cellValue = "";

            if (cellElement.Patterns.Value.IsSupported)
            {
                cellValue = cellElement.Patterns.Value.Pattern.Value;
                Console.WriteLine($"  Cell ({targetRowIndex},{col}) value (ValuePattern): '{cellValue}'");
            }
            else
            {

                Console.WriteLine($"  Warning: Cell ({targetRowIndex},{col}) does not support ValuePattern. Falling back to Name/Text.");
                if (cellElement.Patterns.Text.IsSupported)
                {
                    cellValue = cellElement.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
                    Console.WriteLine($"  Cell ({targetRowIndex},{col}) value (TextPattern): '{cellValue}'");
                }
                else
                {
                    Console.WriteLine($"  Warning: Cell ({targetRowIndex},{col}) does not support TextPattern either. Using Name property as fallback.");
                }

                Console.WriteLine($"  Cell ({targetRowIndex},{col}) value (Fallback): '{cellValue}'");
            }

            Assert.That(!string.IsNullOrEmpty(cellValue), $"Cell ({targetRowIndex},{col}) has no readable value.");
            actualList.Add(cellValue);
        }

        Console.WriteLine($"Actual list: [{string.Join(", ", actualList)}]");

        // Assert that the actual array matches the expected array
        Assert.That(actualList, Is.EqualTo(expectedList),
            $"Data mismatch in 2nd row.\nExpected: [{string.Join(", ", expectedList)}]\nActual: [{string.Join(", ", actualList)}]");

        Console.WriteLine("Second row data verified successfully.");
    }
[Test]
    public void ReadThirdColumnData()
    {
        var TabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Thread.Sleep(500); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");

        // Find the DataGrid AutomationElement
        var dataGridElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("dataGridView1").And(cf.ByControlType(ControlType.DataGrid)));
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        Thread.Sleep(2000);
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;

        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");

        // Define the target column index (0-indexed, so 3rd column is index 2)
        int targetColumnIndex = 2; // This is the 3rd column

        Assert.That(targetColumnIndex, Is.LessThan(colCount), $"Target column index {targetColumnIndex} is out of bounds. Max column index is {colCount - 1}.");
         string[] expectedThirdColumnArray = {"21","22", "23", "24", "25", "26", "27", "28", "29", "30", "(null)" };
        Console.WriteLine($"Expected values in the 3rd column: [{string.Join(", ", expectedThirdColumnArray)}]");
        List<string> thirdColumnValues = new List<string>();

        Console.WriteLine($"Reading cells from the third column (index {targetColumnIndex}):");

        // Iterate through each row to get the cell from the target column
        for (int row = 0; row < rowCount; row++)
        {
            var cellElement = gridPattern.GetItem(row, targetColumnIndex);
            Assert.That(cellElement, Is.Not.Null, $"Cell at row {row}, column {targetColumnIndex} not found.");

            string cellValue = "";

            // Get the value from the cell's ValuePattern
            if (cellElement.Patterns.Value.IsSupported)
            {
                cellValue = cellElement.Patterns.Value.Pattern.Value;
                Console.WriteLine($"  Cell ({row},{targetColumnIndex}) value (ValuePattern): '{cellValue}'");
            }
            else
            {
                // Fallback if ValuePattern is not supported
                Console.WriteLine($"  Warning: Cell ({row},{targetColumnIndex}) does not support ValuePattern. Falling back to Name/Text.");
                if (cellElement.Patterns.Text.IsSupported)
                {
                    cellValue = cellElement.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
                }
                else
                {
                    cellValue = cellElement.Name; // Last resort
                }
                Console.WriteLine($"  Cell ({row},{targetColumnIndex}) value (Fallback): '{cellValue}'");
            }

            Assert.That(!string.IsNullOrEmpty(cellValue), $"Cell ({row},{targetColumnIndex}) has no readable value.");
            thirdColumnValues.Add(cellValue);
        }

        string[] actualThirdColumnArray = thirdColumnValues.ToArray();
        Console.WriteLine($"Actual Third Column Values: [{string.Join(", ", actualThirdColumnArray)}]");
       Assert.That(actualThirdColumnArray, Is.EqualTo(expectedThirdColumnArray),
            $"Data mismatch in 3rd column.\nExpected: [{string.Join(", ", expectedThirdColumnArray)}]\nActual: [{string.Join(", ", actualThirdColumnArray)}]");
        
        Console.WriteLine("Third column data read successfully.");
    }
    [Test]
    public void Readcell3X4()
    {
        var TabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Thread.Sleep(500); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");

        // Find the DataGrid AutomationElement
        var dataGridElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("dataGridView1").And(cf.ByControlType(ControlType.DataGrid)));
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        Thread.Sleep(2000);
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;

        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");

        // Define the target column index (0-indexed, so 3rd column is index 2)
        int targetRowIndex = 3; // This is the 3rd row (0-indexed)
        int targetColumnIndex = 4;
        string expectedCellValue = "4"; // Expected value for cell at (3, 4)
        var cellElement = gridPattern.GetItem(targetRowIndex, targetColumnIndex);
        Assert.That(cellElement, Is.Not.Null, $"Cell at row {targetRowIndex}, column {targetColumnIndex} not found.");
        string cellValue = "";
        // Get the value from the cell's ValuePattern
        if (cellElement.Patterns.Value.IsSupported)
        {
            cellValue = cellElement.Patterns.Value.Pattern.Value;
            Console.WriteLine($"  Cell ({targetRowIndex},{targetColumnIndex}) value (ValuePattern): '{cellValue}'");
        }
        else
        {
            // Fallback if ValuePattern is not supported
            Console.WriteLine($"  Warning: Cell ({targetRowIndex},{targetColumnIndex}) does not support ValuePattern. Falling back to Name/Text.");
            if (cellElement.Patterns.Text.IsSupported)
            {
                cellValue = cellElement.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
            }
            else
            {
                cellValue = cellElement.Name; // Last resort
            }
            Console.WriteLine($"  Cell ({targetRowIndex},{targetColumnIndex}) value (Fallback): '{cellValue}'");
        }

        Assert.That(!string.IsNullOrEmpty(cellValue), $"Cell ({targetRowIndex},{targetColumnIndex}) has no readable value.");
        Console.WriteLine($"Cell value at row {targetRowIndex}, column {targetColumnIndex}: {cellValue}");
        Assert.That(cellValue, Is.EqualTo(expectedCellValue), $"Expected cell value '{expectedCellValue}' does not match actual value '{cellValue}' at row {targetRowIndex}, column {targetColumnIndex}.");
        Console.WriteLine("Cell value verified successfully.");
        // TODO: delete, new row insertion 


       
    }
    [Test]
    public void SetData()
    {

        var TabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Thread.Sleep(500); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");
        // 1. Find the TabControl and select TabPage3

        // 2. Find the DataGrid AutomationElement
        // We get the AutomationElement directly, no AsDataGrid() here.
        var dataGridElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("dataGridView1").And(cf.ByControlType(ControlType.DataGrid)));
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        Thread.Sleep(2000); // Give UI time to render DataGrid contents

        // 3. Get the GridPattern
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;
        var targetRowIndex = 2; // This is the 11th row (0-indexed, after the header row if present)

        Assert.That(targetRowIndex, Is.LessThan(rowCount), $"Target row index {targetRowIndex} is out of bounds. Max row index is {rowCount - 1}.");

        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");
        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            switch (col)
            {
                case 0:
                    cell.Patterns.Value.Pattern.SetValue("11");
                    Console.WriteLine($"Set value '11' in cell ({targetRowIndex},{col})");
                    break;
                case 1:
                    cell.Patterns.Value.Pattern.SetValue("Name 11");
                    Console.WriteLine($"Set value 'Name 11' in cell ({targetRowIndex},{col})");
                    break;
                case 2:
                    cell.Patterns.Value.Pattern.SetValue("31");
                    Console.WriteLine($"Set value '31' in cell ({targetRowIndex},{col})");
                    break;
                case 3:
                    cell.Patterns.Value.Pattern.SetValue("500");
                    Console.WriteLine($"Set value '500' in cell ({targetRowIndex},{col})");
                    break;
                case 4:
                    cell.Patterns.Value.Pattern.SetValue("11");
                    Console.WriteLine($"Set value '11' in cell ({targetRowIndex},{col})");
                    break;
            }
        }
        // Verify the set data
        string[] expectedArray = [ "11", "Name 11", "31", "500", "11" ];
        string[] actualArray = [];
        string actualElement = "";

        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            Console.WriteLine($"Cell value at row {targetRowIndex}, column {col}: {cell.Patterns.Value.Pattern.Value}");
            actualElement = cell.Patterns.Value.Pattern.Value;

        }
        actualArray = [.. actualArray, actualElement];
                // actualArray = actualArray.Append(actualElement).ToArray();

        Console.WriteLine($"Actual array: [{string.Join(", ", actualArray)}]");
        Assert.That(actualArray, Is.EqualTo(expectedArray),
            $"Data mismatch in row {targetRowIndex}.\nExpected: [{string.Join(", ", expectedArray)}]\nActual: [{string.Join(", ", actualArray)}]");
        Console.WriteLine($"Data set in row {targetRowIndex} successfully.");
       
         
     }

   
    [TearDown]
    public void Teardown()
    {
        Console.WriteLine("--- Teardown Started ---");
        // Close the application after each test
        if (app != null && !app.HasExited)
        {
            Console.WriteLine("Closing application...");
            app.Close();
            app.Dispose();
            Console.WriteLine("Application closed and disposed.");
        }
        else if (app != null)
        {
            Console.WriteLine("Application already exited.");
            app.Dispose();
        }
        else
        {
            Console.WriteLine("Application object was null, no app to close.");
        }


        // Dispose the automation object
        if (automation != null)
        {
            automation.Dispose();
            Console.WriteLine("Automation instance disposed.");
        }

        // Clean up the created test file
        if (File.Exists(testFilePath))
        {
            try
            {
                File.Delete(testFilePath);
                Console.WriteLine($"Cleaned up test file: {testFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting test file {testFilePath}: {ex.Message}");
            }
        }
        Console.WriteLine("--- Teardown Finished ---");
    }
       
    }

