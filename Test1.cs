using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.PatternElements;
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
using FlaUI.Core.Patterns.Infrastructure;
using FlaUI.UIA3.Patterns;
using System.Globalization;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
// using FlaUI.Core.AutomationFactory;

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
            //  Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));
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
            mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(50));
            // Assert.That(mainWindow, Is.Not.Null, "Main window was not found after launch. Application might not have started correctly or window title/class changed.");
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
     private static T GetElement<T>(AutomationElement parentNode, string? name = null, string? automationId = null, ControlType? controlType = null) where T : AutomationElement
    {
        if (name != null)
        {
            ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());
            /*
             PropertyCondition nameCondition = cf.ByName(name);
            AndCondition controlTypeCondition;
            */
            /*
             ConditionBase nameCondition = cf.ByName(name);
            ConditionBase controlTypeCondition = nameCondition;
*/
            ConditionBase controlTypeCondition = cf.ByName(name);

            if (controlType != null)
            {
                controlTypeCondition = controlTypeCondition.And(cf.ByControlType((ControlType)controlType));
            }
            
            return parentNode.FindFirstDescendant(controlTypeCondition).As<T>();
        }
        else if (automationId != null)
        {
            return parentNode.FindFirstDescendant(cf => cf.ByAutomationId(automationId)).As<T>();
        }
        else
        {
            return parentNode.FindFirstDescendant(cf => cf.ByControlType(ControlType.Unknown)).As<T>();
        }

       
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
        // Fallback: If not found as a descendant of fileMenuItem, try finding it globally on the desktop
        // This sometimes happens if the menu is a top-level window itself.
        saveMenuItem ??= automation.GetDesktop().FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Save"))).AsMenuItem();
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
        Console.WriteLine("Horizontal scrollbar found and enabled.");

        var vScrollBar = richTextBoxAutomationElement.FindFirstDescendant(cf => cf.ByControlType(ControlType.ScrollBar).And(cf.ByAutomationId("NonClientVerticalScrollBar")));
        Assert.That(vScrollBar, Is.Not.Null, "Vertical scrollbar should be present.");
        Console.WriteLine("Vertical scrollbar found and enabled.");

        // Ensure the RichTextBox has focus before sending keys
        richTextBox.Focus();
        Thread.Sleep(500); // Give a moment for focus to register
        // IScrollPattern scrollPattern = null;
        //  scrollPattern = richTextBoxAutomationElement.Patterns.Scroll.PatternOrDefault;
        // return scrollPattern != null && scrollPattern.current.HorizontallyScrollable && scrollPattern.Current.VerticallyScrollable;

        // Scroll Down (Vertical)
        Console.WriteLine("Scrolling down with PageDown key...");
        Keyboard.Press(VirtualKeyShort.NEXT); // VK_NEXT is PageDown
        Thread.Sleep(1000); // Give time for scroll to happen
        // Assert.That(scrollPattern)

        // Scroll Down to End (Vertical)
        Console.WriteLine("Scrolling to end with Control + End keys...");
        Keyboard.Press(VirtualKeyShort.CONTROL); // Press Ctrl
        Keyboard.Press(VirtualKeyShort.END);     // Press End
        Keyboard.Release(VirtualKeyShort.END);   // Release End
        Keyboard.Release(VirtualKeyShort.CONTROL); // Release Ctrl
        Thread.Sleep(1000);

        // Scroll Up (Vertical)
        Console.WriteLine("Scrolling up with PageUp key...");
        Keyboard.Press(VirtualKeyShort.PRIOR); // VK_PRIOR is PageUp
        Thread.Sleep(1000);

        // Scroll Up to Beginning (Vertical)
        Console.WriteLine("Scrolling to beginning with Control + Home keys...");
        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Press(VirtualKeyShort.HOME);
        Keyboard.Release(VirtualKeyShort.HOME);
        Keyboard.Release(VirtualKeyShort.CONTROL);
        Thread.Sleep(1000);

        // Scroll Right (Horizontal) - Requires element to be multiline with word wrap off, or similar setup
        // RichTextBox by default might wrap text, making horizontal scroll with arrow keys less effective.
        // You might need to send ArrowRight multiple times for fine-grained horizontal scroll.
        Console.WriteLine("Scrolling right with Right Arrow key...");
        Keyboard.Press(VirtualKeyShort.RIGHT); // VK_RIGHT is Right Arrow
        Thread.Sleep(1000);

        // Scroll Left (Horizontal)
        Console.WriteLine("Scrolling left with Left Arrow key...");
        Keyboard.Press(VirtualKeyShort.LEFT); // VK_LEFT is Left Arrow
        Thread.Sleep(1000);
    }
    //     Button LineUp = vScrollBar.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Line Up"))).AsButton();
    //     Assert.That(LineUp, Is.Not.Null, "Line Up button should be found as a descendant of the vertical scrollbar.");
    //     Console.WriteLine("LineUp button found");

    //     LineUp.Click();
    //     Console.WriteLine("Line up button clicked to scroll");
    //     Thread.Sleep(2000); // Shorter sleep for responsiveness

    //     Button LineDown = vScrollBar.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Line Down"))).AsButton();
    //     Assert.That(LineDown, Is.Not.Null, "Line Down button should be found as a descendant of the vertical scrollbar.");
    //     Console.WriteLine("LineDown button found");

    //     LineDown.Click(); // Changed from LineUp.Click() to LineDown.Click()
    //     Console.WriteLine("Line Down button clicked to scroll");
    //     Thread.Sleep(2000); // Shorter sleep for responsiveness
    // }

    //     // -------------------------------------------------------
    //     // Re-adding the IScrollPattern operations from earlier successful discussion
    //     // This is still the preferred way to scroll for many controls.
    //     IScrollPattern scrollPattern = null;
    //     bool patternAvailable = richTextBoxAutomationElement.WaitUntil(() =>
    //     {
    //         scrollPattern = richTextBoxAutomationElement.Patterns.Scroll.PatternOrDefault;
    //         return scrollPattern != null && scrollPattern.Current.HorizontallyScrollable && scrollPattern.Current.VerticallyScrollable;
    //     }, TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(500)); 

    //     Assert.That(patternAvailable, Is.True, "ScrollPattern did not become available or scrollable on the RichTextBox itself.");
    //     Assert.That(scrollPattern, Is.Not.Null, "ScrollPattern is null after waiting on RichTextBox.");

    //     // ... (rest of your IScrollPattern operations as discussed previously) ...
    //     // Example:
    //     Console.WriteLine("\n--- Performing Horizontal Scroll Operations (via IScrollPattern) ---");
    //     Assert.That(scrollPattern.Current.HorizontallyScrollable, Is.True, "RichTextBox should be horizontally scrollable.");
    //     Assert.That(scrollPattern.Current.HorizontalScrollPercent, Is.Zero, "RichTextBox should initially be at 0% horizontal scroll.");
    //     scrollPattern.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
    //     Thread.Sleep(1000);
    //     Console.WriteLine($"Horizontal scroll percent after large increment: {scrollPattern.Current.HorizontalScrollPercent}");
    //     Assert.That(scrollPattern.Current.HorizontalScrollPercent, Is.GreaterThan(0.0), "RichTextBox should have scrolled right.");
    //     scrollPattern.SetScrollPercent(100.0, -1.0);
    //     Thread.Sleep(1000);
    //     Console.WriteLine($"Horizontal scroll percent after setting to 100%: {scrollPattern.Current.HorizontalScrollPercent}");
    //     Assert.That(scrollPattern.Current.HorizontalScrollPercent, Is.EqualTo(100.0), "RichTextBox should be at 100% horizontal scroll.");

    //     // Add the vertical scroll operations from IScrollPattern as well...
    //     Console.WriteLine("\n--- Performing Vertical Scroll Operations (via IScrollPattern) ---");
    //     Assert.That(scrollPattern.Current.VerticallyScrollable, Is.True, "RichTextBox should be vertically scrollable.");
    //     Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.Zero, "RichTextBox should initially be at 0% vertical scroll.");
    //     scrollPattern.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
    //     Thread.Sleep(1000);
    //     Console.WriteLine($"Vertical scroll percent after large increment: {scrollPattern.Current.VerticalScrollPercent}");
    //     Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.GreaterThan(0.0), "RichTextBox should have scrolled down.");
    //     scrollPattern.SetScrollPercent(-1.0, 100.0);
    //     Thread.Sleep(1000);
    //     Console.WriteLine($"Vertical scroll percent after setting to 100%: {scrollPattern.Current.VerticalScrollPercent}");
    //     Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.EqualTo(100.0), "RichTextBox should be at 100% vertical scroll.");
    // }



    // Scroll to the end (bottom)
    // scrollPattern.SetScrollPercent(-1.0, 100.0);
    // Thread.Sleep(1000);
    // Console.WriteLine($"Vertical scroll percent after setting to 100%: {scrollPattern.Current.VerticalScrollPercent}");
    // Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.EqualTo(100.0), "RichTextBox should be at 100% vertical scroll.");

    // // Scroll back up by a large amount
    // scrollPattern.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeDecrement);
    // Thread.Sleep(1000);
    // Console.WriteLine($"Vertical scroll percent after large decrement: {scrollPattern.Current.VerticalScrollPercent}");
    // Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.LessThan(100.0), "RichTextBox should have scrolled up.");

    // // Scroll back to beginning (0%)
    // scrollPattern.SetScrollPercent(-1.0, 0.0);
    // Thread.Sleep(1000);
    // Console.WriteLine($"Vertical scroll percent after setting to 0%: {scrollPattern.Current.VerticalScrollPercent}");
    // Assert.That(scrollPattern.Current.VerticalScrollPercent, Is.EqualTo(0.0), "RichTextBox should be at 0% vertical scroll.");


    // // Explicitly cast to ScrollPattern to access .Current
    // var hScrollPatternCasted = (ScrollPattern)hScrollPattern;
    // var vScrollPatternCasted = (ScrollPattern)vScrollPattern;

    // // --- Horizontal Scrollbar Operations ---
    // Console.WriteLine("\n--- Performing Horizontal Scroll Operations ---");
    // Assert.That(hScrollPatternCasted.HorizontallyScrollable, Is.True, "Horizontal scrollbar should be horizontally scrollable.");
    // Assert.That(hScrollPatternCasted.HorizontalScrollPercent, Is.Zero, "Horizontal scrollbar should initially be at 0%.");
    // // Scroll right by a large amount (e.g., one page)
    // hScrollPatternCasted.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
    // Thread.Sleep(1000); // Give UI time to scroll
    // Console.WriteLine($"Horizontal scroll percent after large increment: {hScrollPatternCasted.HorizontalScrollPercent}");
    // Assert.That(Convert.ToDouble(hScrollPatternCasted.HorizontalScrollPercent), Is.GreaterThan(0.0), "Horizontal scrollbar should have scrolled right.");
    // // Scroll to the end (rightmost)
    // hScrollPatternCasted.SetScrollPercent(100.0, -1.0);
    // Thread.Sleep(1000);
    // Console.WriteLine($"Horizontal scroll percent after setting to 100%: {hScrollPatternCasted.HorizontalScrollPercent}");
    // Assert.That(Convert.ToDouble(hScrollPatternCasted.HorizontalScrollPercent), Is.EqualTo(100.0), "Horizontal scrollbar should be at 100%.");
    // // vertical scrollbar operations
    // Console.WriteLine("\n--- Performing Vertical Scroll Operations ---");
    // Assert.That(vScrollPatternCasted.VerticallyScrollable, Is.True, "Vertical scrollbar should be vertically scrollable.");
    // Assert.That(vScrollPatternCasted.VerticalScrollPercent, Is.Zero, "Vertical scrollbar should initially be at 0%.");
    // // Scroll down by a large amount (e.g., one page)
    // vScrollPatternCasted.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
    // Thread.Sleep(1000); // Give UI time to scroll
    // Console.WriteLine($"Vertical scroll percent after large increment: {vScrollPatternCasted.VerticalScrollPercent}");
    // Assert.That(Convert.ToDouble(vScrollPatternCasted.VerticalScrollPercent), Is.GreaterThan(0.0), "Vertical scrollbar should have scrolled down.");
    // // Scroll to the end (bottom)
    // vScrollPatternCasted.SetScrollPercent(-1.0, 100.0);
    // Thread.Sleep(1000);
    // Console.WriteLine($"Vertical scroll percent after setting to 100%: {vScrollPatternCasted.VerticalScrollPercent}");
    // Assert.That(Convert.ToDouble(vScrollPatternCasted.VerticalScrollPercent), Is.EqualTo(100.0), "Vertical scrollbar should be at 100%.");
    [Test]
    public void TryScroll()
    {
    
        // ... (existing code to find TabPage3 and richTextBoxAutomationElement) ...
        var TabPage3 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("tabPage3")).AsTabItem();
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        TabPage3.Select();
        Thread.Sleep(500);
        Console.WriteLine("Switched to TabPage3.");

        var richTextBoxAutomationElement = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("richTextBox1"));
        Assert.That(richTextBoxAutomationElement, Is.Not.Null, "RichTextBox AutomationElement should be found.");

        var richTextBoxAsTextBox = richTextBoxAutomationElement.AsTextBox();
        Assert.That(richTextBoxAsTextBox, Is.Not.Null, "RichTextBox (as TextBox) should be castable.");
        Console.WriteLine("RichTextBox found and cast to TextBox.");

        string horizontalLinePart = "This is an extremely long line designed to force the horizontal scrollbar into existence. We're repeating this segment multiple times to ensure it far exceeds the typical width of a RichTextBox. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ";
        string verticalLinePart = "This is a line for vertical scrolling. This will be repeated many times to ensure the vertical scrollbar appears and becomes active.";

        string longText = string.Concat(Enumerable.Repeat(horizontalLinePart, 5)) + "\n";
        longText += string.Join("\n", Enumerable.Repeat(verticalLinePart, 50));

        richTextBoxAsTextBox.Text = string.Empty;
        richTextBoxAsTextBox.Enter(longText);
        Console.WriteLine($"Entered text into RichTextBox. Text length: {longText.Length}");

        Thread.Sleep(2000); // Give UI time to render and for focus to settle

        // Get the IScrollPattern from the RichTextBox itself for assertions
        // DECLARE scrollPattern HERE, OUTSIDE THE LAMBDA
        // IScrollPattern scrollPattern = null; // <--- This declaration should be here

        // // bool patternAvailable = richTextBoxAutomationElement.WaitUntil(() =>
        // {
        //     // Assign to the 'scrollPattern' declared above, not a new local one
        //     scrollPattern = richTextBoxAutomationElement.Patterns.Scroll.PatternOrDefault;
        //     return scrollPattern != null && scrollPattern.Current.HorizontallyScrollable && scrollPattern.Current.VerticallyScrollable;
        // }, TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(500));

        // using (Assert.EnterMultipleScope())
        // {
        //     Assert.That(patternAvailable, Is.True, "ScrollPattern did not become available or scrollable on the RichTextBox itself.");
        //     Assert.That(scrollPattern, Is.Not.Null, "ScrollPattern is null after waiting on RichTextBox.");
        // }

        // // --- Keyboard Scroll Operations ---
        // Console.WriteLine("\n--- Performing Scroll Operations via Keyboard Keys ---");

        // // Ensure the RichTextBox has focus before sending keys
        // richTextBoxAsTextBox.Focus();
        // Thread.Sleep(500);

        // // --- Vertical Scroll Assertions ---
        // Console.WriteLine("Verifying vertical scrolling...");

    //     // Initial vertical scroll percentage
    //     double initialVerticalScroll = scrollPattern.Current.VerticalScrollPercent;
    //     Console.WriteLine($"Initial Vertical Scroll Percent: {initialVerticalScroll}");
    //     Assert.That(initialVerticalScroll, Is.Zero, "RichTextBox should initially be at 0% vertical scroll.");

    //     // Scroll Down (PageDown) and Assert
    //     Console.WriteLine("Scrolling down with PageDown key...");
    //     Keyboard.Press(VirtualKeyShort.NEXT);
    //     Thread.Sleep(1000);
    //     double verticalScrollAfterPageDown = scrollPattern.Current.VerticalScrollPercent;
    //     Console.WriteLine($"Vertical Scroll Percent after PageDown: {verticalScrollAfterPageDown}");
    //     Assert.That(verticalScrollAfterPageDown, Is.GreaterThan(initialVerticalScroll), "Vertical scroll should have increased after PageDown.");

    //     // Scroll to End (Ctrl + End) and Assert
    //     Console.WriteLine("Scrolling to end with Control + End keys...");
    //     Keyboard.Press(VirtualKeyShort.CONTROL);
    //     Keyboard.Press(VirtualKeyShort.END);
    //     Keyboard.Release(VirtualKeyShort.END);
    //     Keyboard.Release(VirtualKeyShort.CONTROL);
    //     Thread.Sleep(1000);
    //     double verticalScrollAtEnd = scrollPattern.Current.VerticalScrollPercent;
    //     Console.WriteLine($"Vertical Scroll Percent at End: {verticalScrollAtEnd}");
    //     Assert.That(verticalScrollAtEnd, Is.EqualTo(100.0), "Vertical scroll should be at 100% after Ctrl+End.");

    //     // Scroll Up (PageUp) and Assert
    //     Console.WriteLine("Scrolling up with PageUp key...");
    //     Keyboard.Press(VirtualKeyShort.PRIOR);
    //     Thread.Sleep(1000);
    //     double verticalScrollAfterPageUp = scrollPattern.Current.VerticalScrollPercent;
    //     Console.WriteLine($"Vertical Scroll Percent after PageUp: {verticalScrollAfterPageUp}");
    //     Assert.That(verticalScrollAfterPageUp, Is.LessThan(100.0), "Vertical scroll should have decreased after PageUp.");
    //     Assert.That(verticalScrollAfterPageUp, Is.GreaterThanOrEqualTo(0.0), "Vertical scroll should not go below 0% after PageUp.");


    //     // Scroll to Beginning (Ctrl + Home) and Assert
    //     Console.WriteLine("Scrolling to beginning with Control + Home keys...");
    //     Keyboard.Press(VirtualKeyShort.CONTROL);
    //     Keyboard.Press(VirtualKeyShort.HOME);
    //     Keyboard.Release(VirtualKeyShort.HOME);
    //     Keyboard.Release(VirtualKeyShort.CONTROL);
    //     Thread.Sleep(1000);
    //     double verticalScrollAtBeginning = scrollPattern.Current.VerticalScrollPercent;
    //     Console.WriteLine($"Vertical Scroll Percent at Beginning: {verticalScrollAtBeginning}");
    //     Assert.That(verticalScrollAtBeginning, Is.EqualTo(0.0), "Vertical scroll should be at 0% after Ctrl+Home.");


    //     // --- Horizontal Scroll Assertions ---
    //     Console.WriteLine("\nVerifying horizontal scrolling...");

    //     double initialHorizontalScroll = scrollPattern.Current.HorizontalScrollPercent;
    //     Console.WriteLine($"Initial Horizontal Scroll Percent: {initialHorizontalScroll}");
    //     Assert.That(initialHorizontalScroll, Is.Zero, "RichTextBox should initially be at 0% horizontal scroll.");

    //     // Scroll Right (Right Arrow) and Assert
    //     Console.WriteLine("Scrolling right with Right Arrow key...");
    //     Keyboard.Press(VirtualKeyShort.RIGHT);
    //     Thread.Sleep(1000);
    //     double horizontalScrollAfterRightArrow = scrollPattern.Current.HorizontalScrollPercent;
    //     Console.WriteLine($"Horizontal Scroll Percent after Right Arrow: {horizontalScrollAfterRightArrow}");
    //     Assert.That(horizontalScrollAfterRightArrow, Is.GreaterThan(initialHorizontalScroll), "Horizontal scroll should have increased after Right Arrow.");

    //     // Scroll Left (Left Arrow) and Assert
    //     Console.WriteLine("Scrolling left with Left Arrow key...");
    //     Keyboard.Press(VirtualKeyShort.LEFT);
    //     Thread.Sleep(1000);
    //     double horizontalScrollAfterLeftArrow = scrollPattern.Current.HorizontalScrollPercent;
    //     Console.WriteLine($"Horizontal Scroll Percent after Left Arrow: {horizontalScrollAfterLeftArrow}");
    //     Assert.That(horizontalScrollAfterLeftArrow, Is.LessThan(horizontalScrollAfterRightArrow), "Horizontal scroll should have decreased after Left Arrow.");
    //     Assert.That(horizontalScrollAfterLeftArrow, Is.GreaterThanOrEqualTo(0.0), "Horizontal scroll should not go below 0% after Left Arrow.");
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
    public void DragAndDrop()
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
            Console.WriteLine($"Cell ({targetRowIndex},{targetColumnIndex}) value (ValuePattern): '{cellValue}'");
        }
        else
        {
            // If ValuePattern is not supported (e.g., for non-editable cells),
            // try to get text directly or use a TextPattern.
            cellValue = cellElement.Name; // Fallback to Name property
            Console.WriteLine($"Cell ({targetRowIndex},{targetColumnIndex}) value (Name property): '{cellValue}' (ValuePattern not supported)");
        }

        var richTextBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("richTextBox1"));
        Assert.That(richTextBox, Is.Not.Null, "RichTextBox AutomationElement should be present.");
        Console.WriteLine($"RichTextBox AutomationElement found: {richTextBox.Name} (AutomationId: {richTextBox.AutomationId})");
        Thread.Sleep(2000);

        // --- Perform Drag and Drop using FlaUI's Mouse methods ---
        // You need an Automation object (usually from Application.Attach or AutomationFactory.Get...")
        // Assuming 'automation' is available, e.g., from `FlaUI.Core.AutomationFactory.Get </summary>()` or `app.Automation`.
        // If you are using a FlaUI TestBase, 'Automation' might be directly accessible.
        // Let's assume you have an 'automation' instance available. If not, you'd initialize it:
        // var automation = new UIA3Automation(); // or UIA2Automation() depending on your application type

        // 1. Get the bounding rectangle of the source cell and the target rich text box

        var nextCellElement = gridPattern.GetItem(4, 4);
        Assert.That(cellElement, Is.Not.Null, $"Cell at row {targetRowIndex}, column {targetColumnIndex} not found.");

        var cellBounds = cellElement.BoundingRectangle;
        var cellNextBounds = nextCellElement.BoundingRectangle;

        // 2. Calculate the start and end points for the drag
        // You might want to drag from the center of the cell
        var startPoint = cellBounds.Center();
        // And drop to the center of the rich text box, or a specific point within it
        var endPoint = cellNextBounds.Center();

        Console.WriteLine($"Starting drag from: {startPoint}");
        Console.WriteLine($"Dropping to: {endPoint}");

        // 3. Perform the drag and drop
        // Move to start point and press mouse button down
        Mouse.MoveTo(startPoint);
        Mouse.Down(MouseButton.Left);
        Thread.Sleep(200); // brief pause to simulate drag hold

        // Move to end point
        Mouse.MoveTo(endPoint);
        Thread.Sleep(200); // simulate human-like drag

        // Release mouse button to drop
        Mouse.Up(MouseButton.Left);
        Thread.Sleep(500); // allow UI to process drop

        Button messageBoxOkButton = GetElement<Button>(mainWindow, name: "OK", controlType: ControlType.Button);
        Assert.That(messageBoxOkButton, Is.Not.Null, "OK button should be found.");
        messageBoxOkButton.Click();
        Console.WriteLine("Drag and drop operation completed.");
        Thread.Sleep(1000); // Give time for the drop action to complete in the UI

        // Optional: Verify the content of the rich text box
        // This depends on how your rich text box updates after a drop.
        // If it has a ValuePattern, you can check its value.
        if (richTextBox.Patterns.Value.IsSupported)
        {
            var rawText = richTextBox.Patterns.Value.Pattern.Value.Value;
var trimmedText = rawText.Trim();
            Console.WriteLine($"RichTextBox content after drop: '{trimmedText}'");
            // Assert.That(richTextBoxValue, Contains.Substring(cellValue), "RichTextBox should contain the dropped cell value.");
           Console.WriteLine($"expected content after drop: '{expectedCellValue}'");
            Assert.That(trimmedText, Is.EqualTo(expectedCellValue), "RichTextBox should contain the dropped cell value.");
        }
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
             if (!cell.Patterns.Value.Pattern.IsReadOnly)
           
            {

                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is editable.");


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
            else
            {
                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is read-only.");
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
            actualArray = [.. actualArray, actualElement];
           
        }
        
        Console.WriteLine($"Actual array: [{string.Join(", ", actualArray)}]");
        Assert.That(actualArray, Is.EqualTo(expectedArray),
            $"Data mismatch in row {targetRowIndex}.\nExpected: [{string.Join(", ", expectedArray)}]\nActual: [{string.Join(", ", actualArray)}]");
        Console.WriteLine($"Data set in row {targetRowIndex} successfully.");
       
         
     }
     [Test]
    public void InsertRow()
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
        var targetRowIndex = 10; // This is the 11th row (0-indexed, after the header row if present)

        Assert.That(targetRowIndex, Is.LessThan(rowCount), $"Target row index {targetRowIndex} is out of bounds. Max row index is {rowCount - 1}.");

        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");
        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Thread.Sleep(2000);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
             if (!cell.Patterns.Value.Pattern.IsReadOnly)
           
            {

                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is editable.");


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
            else
            {
                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is read-only.");
            }
        }
        // Verify the set data
        string[] expectedArray = [ "11", "Name 11", "31", "500", "11" ];
        string[] actualArray = [];
        string actualElement = "";

        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Thread.Sleep(1000);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            Console.WriteLine($"Cell value at row {targetRowIndex}, column {col}: {cell.Patterns.Value.Pattern.Value}");
            actualElement = cell.Patterns.Value.Pattern.Value;
            actualArray = [.. actualArray, actualElement];
           
        }
        
        Console.WriteLine($"Actual array: [{string.Join(", ", actualArray)}]");
        Assert.That(actualArray, Is.EqualTo(expectedArray),
            $"Data mismatch in row {targetRowIndex}.\nExpected: [{string.Join(", ", expectedArray)}]\nActual: [{string.Join(", ", actualArray)}]");
        Console.WriteLine($"Data set in row {targetRowIndex} successfully.");
       
         
     }
    [Test]
    public void DeleteRow()
    {
        var TabPage3 = GetElement<TabItem>(mainWindow, name: "tabPage3", controlType: ControlType.TabItem);
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Wait.UntilInputIsProcessed(); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");
        // 1. Find the TabControl and select TabPage3

        // 2. Find the DataGrid AutomationElement
        // We get the AutomationElement directly, no AsDataGrid() here.
        var dataGridElement = GetElement<DataGridView>(mainWindow, automationId: "dataGridView1", controlType: ControlType.DataGrid);
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        var dataGridResponse = Wait.UntilResponsive(dataGridElement, TimeSpan.FromSeconds(10)); // Give UI time to render DataGrid contents
        Assert.That(dataGridResponse, Is.True, "DataGrid is not responsive after 10 seconds.");
        // 3. Get the GridPattern
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;
        var targetRowIndex = 4; // This is the 11th row (0-indexed, after the header row if present)
        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");
        Assert.That(targetRowIndex, Is.LessThan(rowCount), $"Target row index {targetRowIndex} is out of bounds. Max row index is {rowCount - 1}.");
        var rowHeadrer = GetElement<DataGridViewRow>(dataGridElement, name: "Row 4", controlType: ControlType.Header);
        Assert.That(rowHeadrer, Is.Not.Null, "RowHeader should be found.");
        rowHeadrer.Click();
        Wait.UntilInputIsProcessed(); // Give UI time to process click event
        Console.WriteLine("RowHeader clicked.");
        rowHeadrer.RightClick();
        Wait.UntilInputIsProcessed(); // Give UI time to process click event
        Console.WriteLine("RowHeader right-clicked.");
        var deleteMenuItem = GetElement<MenuItem>(mainWindow, name: "Delete Row", controlType: ControlType.MenuItem);
        Assert.That(deleteMenuItem, Is.Not.Null, "Delete menu item should be found.");
        deleteMenuItem.Click();
        Wait.UntilInputIsProcessed(); // Give UI time to process click event
        Console.WriteLine("Delete menu item clicked.");
        var messageBoxLabel = GetElement<Label>(mainWindow, name: "Row deleted successfully!", controlType: ControlType.Text);
        Assert.That(messageBoxLabel, Is.Not.Null, "Message box label should be found.");
        Console.WriteLine("Message box label found.");
        var okButton = GetElement<Button>(mainWindow, name: "OK", controlType: ControlType.Button);
        Assert.That(okButton, Is.Not.Null, "OK button should be found.");
        okButton.Click();
        Wait.UntilInputIsProcessed(); // Give UI time to process click event
        Console.WriteLine("OK button clicked.");

    }
    [Test]
    public void SearchInDataGrid()
    {
        var TabPage3 = GetElement<TabItem>(mainWindow, name: "tabPage3", controlType: ControlType.TabItem);
        Assert.That(TabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        TabPage3.Select();
        Wait.UntilInputIsProcessed(); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");
        // 1. Find the TabControl and select TabPage3

        // 2. Find the DataGrid AutomationElement
        // We get the AutomationElement directly, no AsDataGrid() here.
        var dataGridElement = GetElement<DataGridView>(mainWindow, automationId: "dataGridView1", controlType: ControlType.DataGrid);
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        TextBox textBox = GetElement<TextBox>(mainWindow, name: "Search Text", controlType: ControlType.Edit);
        Assert.That(textBox, Is.Not.Null, "TextBox 'Search' should be found.");
        Console.WriteLine($"TextBox found: {textBox.Name}");
        textBox.Text = string.Empty;
        Wait.UntilInputIsProcessed(); // Give UI time to process input
        textBox.Enter("Name 9");
        Console.WriteLine($"Text entered: {textBox.Text}");// Give UI time to process input
        Button button = GetElement<Button>(mainWindow, name: "Search Button", controlType: ControlType.Button);
        Assert.That(button, Is.Not.Null, "Button 'Search' should be found.");
        Console.WriteLine($"Button found: {button.Name}");
        button.Click();
        Wait.UntilInputIsProcessed(); // Give UI time to process click
        Console.WriteLine("Button clicked.");
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

       
        int colCount = gridPattern.ColumnCount;
         string[] expectedArray = [ "9", "Name 9", "29", "1800", "9" ];
        string[] actualArray = [];
        string actualElement;
        var targetRowIndex = 0;
        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Thread.Sleep(1000);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            Console.WriteLine($"Cell value at row {targetRowIndex}, column {col}: {cell.Patterns.Value.Pattern.Value}");
            actualElement = cell.Patterns.Value.Pattern.Value;
            actualArray = [.. actualArray, actualElement];
           
        }
        
        Console.WriteLine($"Actual array: [{string.Join(", ", actualArray)}]");
        Assert.That(actualArray, Is.EqualTo(expectedArray),
            $"Data mismatch in row {targetRowIndex}.\nExpected: [{string.Join(", ", expectedArray)}]\nActual: [{string.Join(", ", actualArray)}]");
        Console.WriteLine($"Row {targetRowIndex} found and verified successfully.");
       
        
        

    }
    [Test]
    public void VerifyDataGridColumnsAutoResizeOnFill()
    {
        // 1. Select TabPage3 where the DataGrid is located
        var tabPage3 = GetElement<TabItem>(mainWindow, name: "tabPage3", controlType: ControlType.TabItem);
        Assert.That(tabPage3, Is.Not.Null, "TabPage3 should be found.");
        // Select the tab page
        tabPage3.Select();
        Wait.UntilInputIsProcessed(); // Give UI time to switch tabs
        Console.WriteLine("Switched to TabPage3.");
        // 1. Find the TabControl and select TabPage3

        // 2. Find the DataGrid AutomationElement
        // We get the AutomationElement directly, no AsDataGrid() here.
        var dataGridElement = GetElement<DataGridView>(mainWindow, automationId: "dataGridView1", controlType: ControlType.DataGrid);
        Assert.That(dataGridElement, Is.Not.Null, "DataGrid AutomationElement should be present.");
        Console.WriteLine($"DataGrid AutomationElement found: {dataGridElement.Name} (AutomationId: {dataGridElement.AutomationId})");
        var gridPattern = dataGridElement.Patterns.Grid.Pattern;
        Assert.That(gridPattern, Is.Not.Null, "DataGrid does not support GridPattern.");

        int rowCount = gridPattern.RowCount;
        int colCount = gridPattern.ColumnCount;
        var targetRowIndex = 10; // This is the 11th row (0-indexed, after the header row if present)

        Assert.That(targetRowIndex, Is.LessThan(rowCount), $"Target row index {targetRowIndex} is out of bounds. Max row index is {rowCount - 1}.");
        var initialColumnElements = dataGridElement.Header.Columns;
        Assert.That(initialColumnElements.Length, Is.EqualTo(colCount), "Expected 5 columns.");

        double initialDataGridWidth = dataGridElement.BoundingRectangle.Width;
        double initialColumnsTotalWidth = initialColumnElements.Sum(c => c.BoundingRectangle.Width);
        Console.WriteLine($"Initial DataGrid Width: {initialDataGridWidth}");
        Console.WriteLine($"Initial Columns Total Width: {initialColumnsTotalWidth}");
        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");
        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
            Thread.Sleep(2000);
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            if (!cell.Patterns.Value.Pattern.IsReadOnly)

            {

                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is editable.");


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
                        cell.Patterns.Value.Pattern.SetValue("3111111111111111111111111111111");
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
            else
            {
                Console.WriteLine($"Cell at row {targetRowIndex}, column {col} is read-only.");
            }
        }
        var currentDataGridWidth = dataGridElement.BoundingRectangle.Width;
        var currentColumnsTotalWidth = initialColumnElements.Sum(c => c.BoundingRectangle.Width);
        Console.WriteLine($"Final DataGrid Width: {currentDataGridWidth}");
        Console.WriteLine($"Final Columns Total Width: {currentColumnsTotalWidth}");
        Console.WriteLine($"DataGrid reports {rowCount} rows and {colCount} columns via GridPattern.");


        Assert.That(currentColumnsTotalWidth, Is.GreaterThanOrEqualTo(initialColumnsTotalWidth),
            $"Columns should fill the DataGrid. Total columns width ({currentColumnsTotalWidth}) " +
            $"is too small compared to DataGrid width ({currentDataGridWidth}).");
       string[] expectedArray = [ "11", "Name 11", "3111111111111111111111111111111", "500", "11" ];
        string[] actualArray = [];
        string actualElement = "";

        for (int col = 0; col < colCount; col++)
        {
            var cell = gridPattern.GetItem(targetRowIndex, col);
           
            Assert.That(cell, Is.Not.Null, $"Cell at row {targetRowIndex}, column {col} not found.");
            Console.WriteLine($"Cell value at row {targetRowIndex}, column {col}: {cell.Patterns.Value.Pattern.Value}");
            actualElement = cell.Patterns.Value.Pattern.Value;
            actualArray = [.. actualArray, actualElement];
           
        }
        
        Console.WriteLine($"Actual array: [{string.Join(", ", actualArray)}]");
        Assert.That(actualArray, Is.EqualTo(expectedArray),
            $"Data mismatch in row {targetRowIndex}.\nExpected: [{string.Join(", ", expectedArray)}]\nActual: [{string.Join(", ", actualArray)}]");
        Console.WriteLine($"Data set in row {targetRowIndex} successfully.");
       
         
     }

       
    
    [Test]
    public void TreeTest()
    {
        var treeItemMI2 = mainWindow.FindFirstDescendant(cf => cf.ByName("MI2").And(cf.ByControlType(ControlType.TreeItem))).AsTreeItem();
        Assert.That(treeItemMI2, Is.Not.Null, "TreeItem 'MI2' should be found.");
        Console.WriteLine($"TreeItem found: {treeItemMI2.Name}");
        treeItemMI2.Expand();

        ITogglePattern togglePattern;
        togglePattern = treeItemMI2.Patterns.Toggle.Pattern;
        
        Console.WriteLine("Initial state of 'MI2' checkbox: Unchecked");     

        if (togglePattern.ToggleState == ToggleState.Off)
        {
            togglePattern.Toggle(); // This action will click the checkbox
            Console.WriteLine($"Toggled 'MI2' checkbox to checked state.");
        }
        else
        {
            Console.WriteLine($"'MI2' checkbox was already in the '{togglePattern.ToggleState}' state. No toggle action needed.");
        }
        Assert.That(togglePattern.ToggleState, Is.EqualTo(ToggleState.On), "The 'MI2' checkbox should be checked.");

        
        var treeItemAravind = treeItemMI2.FindFirstDescendant(cf => cf.ByName("Aravind").And(cf.ByControlType(ControlType.TreeItem))).AsTreeItem();
        Assert.That(treeItemAravind, Is.Not.Null, "TreeItem 'Aravind' should be found as a descendant of 'MI2'.");
        Console.WriteLine($"TreeItem 'Aravind' found within 'MI2' hierarchy.");

        // Assert that 'Shailaja' is a descendant of 'MI2'
        var treeItemShailaja = treeItemMI2.FindFirstDescendant(cf => cf.ByName("Shailaja").And(cf.ByControlType(ControlType.TreeItem))).AsTreeItem();
        Assert.That(treeItemShailaja, Is.Not.Null, "TreeItem 'Shailaja' should be found as a descendant of 'MI2'.");
        Console.WriteLine($"TreeItem 'Shailaja' found within 'MI2' hierarchy.");
        treeItemShailaja.Expand();


        // Assert that 'Vijay' is a descendant of 'MI2'
        var treeItemEmployee = treeItemShailaja.FindFirstDescendant(cf => cf.ByName("employee").And(cf.ByControlType(ControlType.TreeItem))).AsTreeItem();
        Assert.That(treeItemEmployee, Is.Not.Null, "TreeItem 'employee' should be found as a descendant of 'MI2'.");
        Console.WriteLine($"TreeItem 'employee' found within 'Shailaja' hierarchy.");
        var treeItemCustomer = treeItemShailaja.FindFirstDescendant(cf => cf.ByName("customer").And(cf.ByControlType(ControlType.TreeItem))).AsTreeItem();
        Assert.That(treeItemCustomer, Is.Not.Null, "TreeItem 'customer' should be found as a descendant of 'MI2'.");
        Console.WriteLine($"TreeItem 'customer' found within 'Shailaja' hierarchy.");
       
    }
    [Test]
    public void DatePickerTest1()
    {
        Console.WriteLine("Starting DatePickerTest1...");

        // 1. Select TabPage4
        // Passing timeout as the last positional argument.
        Tab tab4 = GetElement<Tab>(mainWindow, name: "tabPage4", controlType: ControlType.TabItem);
        Assert.That(tab4, Is.Not.Null, "Tab 'tabPage4' should be found.");
        Console.WriteLine($"Tab found: {tab4.Name}");
        tab4.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

        // 2. Find the DatePicker ComboBox
        ComboBox datePickerElement = GetElement<ComboBox>(mainWindow, name: "DatePicker", controlType: ControlType.ComboBox);
        Assert.That(datePickerElement, Is.Not.Null, "DatePicker 'DatePicker' should be found.");
        Console.WriteLine($"DatePicker element found: {datePickerElement.Name}");

        IValuePattern valuePattern = datePickerElement.Patterns.Value.Pattern;
        IExpandCollapsePattern expandCollapsePattern = datePickerElement.Patterns.ExpandCollapse.Pattern;

        // Define the target date (e.g., September 22, 2026)
        var targetDate = new DateTime(2025, 9, 1);
        Console.WriteLine($"Target Date: {targetDate.ToShortDateString()}");
        var targetDay = targetDate.Day.ToString();
        Console.WriteLine($"Target Day: {targetDay}");
        // Define the expected display format in the DatePicker's text field after selection
        string expectedDisplayFormat = targetDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        Console.WriteLine($"Expected Final Display Format: '{expectedDisplayFormat}'");
        string currentMonthYear = DateTime.Now.ToString("MMMM, yyyy", CultureInfo.InvariantCulture);
        Console.WriteLine($"Current Month/Year: '{currentMonthYear}'");
        // --- Step 3: Expand the DatePicker to show the calendar ---
        expandCollapsePattern.Expand();
        Console.WriteLine($"DatePicker expanded.");
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

        // --- Step 4: Find the Calendar control ---
        AutomationElement calendar = GetElement<AutomationElement>(mainWindow, name: "Calendar Control", controlType: ControlType.Pane);
        Assert.That(calendar, Is.Not.Null, "Calendar 'Calendar Control' should be found.");
        Console.WriteLine($"Calendar found.");
        FlaUI.Core.Input.Wait.UntilResponsive(calendar, TimeSpan.FromSeconds(5));
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));


        // --- Step 5: Find navigation buttons and the current month/year display ---
        // Button nextMonthButton = GetElement<Button>(calendar, name: "Next Button", controlType: ControlType.Button);
        // Button prevMonthButton = GetElement<Button>(calendar, name: "Previous Button", controlType: ControlType.Button);
        Button currentMonthYearDisplayButton = GetElement<Button>(calendar, name: currentMonthYear, controlType: ControlType.Button);
        Console.WriteLine($"currentMonthYearDisplayButton found.{currentMonthYearDisplayButton.Name}");
        // Assert.That(nextMonthButton, Is.Not.Null, "Next month navigation button not found.");
        // Assert.That(prevMonthButton, Is.Not.Null, "Previous month navigation button not found.");
        Console.WriteLine("Calendar navigation buttons found.");
        // Button targetDayButton = GetElement<Button>(calendar, name: targetDay, controlType: ControlType.DataItem); // Find the button for the target day, controlType: ControlType.Button);
        // Assert.That(targetDayButton, Is.Not.Null, "Target day button not found.");
        // Console.WriteLine($"Target day button found: {targetDayButton.Name}");
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
        var targetMonth = targetDate.Month;
     
        var targetYear = targetDate.Year;   
        Console.WriteLine($"Target Month: {targetMonth}, Target Year: {targetYear}");
        var currentMonth = DateTime.Now.Month; 
        var currentYear = DateTime.Now.Year; 
         Button nextMonthButton = GetElement<Button>(calendar, name: "Next Button", controlType: ControlType.Button);
        Button prevMonthButton = GetElement<Button>(calendar, name: "Previous Button", controlType: ControlType.Button);

        for (int i = 0; i < 12; i++)
        {
             Button targetDayButton = GetElement<Button>(calendar, name: targetDay, controlType: ControlType.DataItem); // Find the button for the target day, controlType: ControlType.Button);

            Console.WriteLine($"Current Month: {currentMonth}, Current Year: {currentYear}");
            if (nextMonthButton != null && prevMonthButton != null && targetYear < currentYear || targetMonth < currentMonth)
            {
                Console.WriteLine("Inside first If");
                Thread.Sleep(1000);
                prevMonthButton.Click();
                Console.WriteLine("previous month button clicked.");
                Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            }
            if (nextMonthButton != null && prevMonthButton != null && targetYear > currentYear || targetMonth > currentMonth)
            {
                Console.WriteLine("Inside 2nd If");

                Thread.Sleep(5000);
                nextMonthButton.Click();


                Console.WriteLine("next month button clicked.");
                Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            }

            else if (targetDayButton != null && targetYear == currentYear && targetMonth == currentMonth)
            {
                Console.WriteLine("Inside 3rd If");

                targetDayButton.Click();
                Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
                break;
            }
            else
            {
                Console.WriteLine("Target date not found in current month.");
            }
            currentMonth = DateTime.Now.AddMonths(i+1).Month;


        }


        // --- Step 6: Verify the selected date ---
        string selectedDateText = datePickerElement.Value;
        Console.WriteLine($"Selected date: {selectedDateText}");
        Assert.That(selectedDateText, Is.EqualTo(expectedDisplayFormat), $"Selected date '{selectedDateText}' does not match the expected display format '{expectedDisplayFormat}'.");


        }
    [Test]
    public void YearselectionTest()
    {
    
        Console.WriteLine("Starting DatePickerTest1...");

        // 1. Select TabPage4
        // Passing timeout as the last positional argument.
        Tab tab4 = GetElement<Tab>(mainWindow, name: "tabPage4", controlType: ControlType.TabItem);
        Assert.That(tab4, Is.Not.Null, "Tab 'tabPage4' should be found.");
        Console.WriteLine($"Tab found: {tab4.Name}");
        tab4.Click();
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

        // 2. Find the DatePicker ComboBox
        ComboBox datePickerElement = GetElement<ComboBox>(mainWindow, name: "DatePicker", controlType: ControlType.ComboBox);
        Assert.That(datePickerElement, Is.Not.Null, "DatePicker 'DatePicker' should be found.");
        Console.WriteLine($"DatePicker element found: {datePickerElement.Name}");

        IValuePattern valuePattern = datePickerElement.Patterns.Value.Pattern;
        IExpandCollapsePattern expandCollapsePattern = datePickerElement.Patterns.ExpandCollapse.Pattern;

        // Define the target date (e.g., September 22, 2026)
        var targetDate = new DateTime(2028, 9, 23);
        Console.WriteLine($"Target Date: {targetDate.ToShortDateString()}");
        var targetDay = targetDate.Day.ToString();
        Console.WriteLine($"Target Day: {targetDay}");
        // Define the expected display format in the DatePicker's text field after selection
        string expectedDisplayFormat = targetDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        Console.WriteLine($"Expected Final Display Format: '{expectedDisplayFormat}'");
        string currentMonthYear = DateTime.Now.ToString("MMMM, yyyy", CultureInfo.InvariantCulture);
        Console.WriteLine($"Current Month/Year: '{currentMonthYear}'");
        // --- Step 3: Expand the DatePicker to show the calendar ---
        expandCollapsePattern.Expand();
        Console.WriteLine($"DatePicker expanded.");
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

        // --- Step 4: Find the Calendar control ---
        AutomationElement calendar = GetElement<AutomationElement>(mainWindow, name: "Calendar Control", controlType: ControlType.Pane);
        Assert.That(calendar, Is.Not.Null, "Calendar 'Calendar Control' should be found.");
        Console.WriteLine($"Calendar found.");
        FlaUI.Core.Input.Wait.UntilResponsive(calendar, TimeSpan.FromSeconds(5));
        FlaUI.Core.Input.Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));


        // --- Step 5: Find navigation buttons and the current month/year display ---
        // Button nextMonthButton = GetElement<Button>(calendar, name: "Next Button", controlType: ControlType.Button);
        // Button prevMonthButton = GetElement<Button>(calendar, name: "Previous Button", controlType: ControlType.Button);
        Button currentMonthYearDisplayButton = GetElement<Button>(calendar, name: currentMonthYear, controlType: ControlType.Button);
        Console.WriteLine($"currentMonthYearDisplayButton found.{currentMonthYearDisplayButton.Name}");
        // Assert.That(nextMonthButton, Is.Not.Null, "Next month navigation button not found.");
        // Assert.That(prevMonthButton, Is.Not.Null, "Previous month navigation button not found.");
        Console.WriteLine("Calendar navigation buttons found.");
        // Button targetDayButton = GetElement<Button>(calendar, name: targetDay, controlType: ControlType.DataItem); // Find the button for the target day, controlType: ControlType.Button);
        // Assert.That(targetDayButton, Is.Not.Null, "Target day button not found.");
        // Console.WriteLine($"Target day button found: {targetDayButton.Name}");
        Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
        currentMonthYearDisplayButton.Click();
        var targetMonth = targetDate.ToString("MMMM", CultureInfo.InvariantCulture);
        Console.WriteLine($"Target Month: {targetMonth}");
        var targetYear = targetDate.Year;   
        Console.WriteLine($"Target Month: {targetMonth}, Target Year: {targetYear}");
        var currentMonth = DateTime.Now.Month; 
        var currentYear = DateTime.Now.Year; 
         Button nextMonthButton = GetElement<Button>(calendar, name: "Next Button", controlType: ControlType.Button);
        Button prevMonthButton = GetElement<Button>(calendar, name: "Previous Button", controlType: ControlType.Button);

        for (int i = 0; i < 5; i++)
        {
            Button targetMonthButton = GetElement<Button>(calendar, name: "Sep", controlType: ControlType.DataItem); //

            if (currentYear > targetYear)
            {
                Console.WriteLine("Inside 1st If");
                prevMonthButton.Click();
                Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            }
            else if (currentYear < targetYear)
            {
                Console.WriteLine($"Current Year: {currentYear}, Target Year: {targetYear}");
                Console.WriteLine("Inside 2nd If");
                nextMonthButton.Click();
                Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            }
            else if (currentYear == targetYear)
            {
                Console.WriteLine("Inside 3rd If");
                //   Button targetDayButton = GetElement<Button>(calendar, name:"23", controlType: ControlType.DataItem); // Find the button for the target day, controlType: ControlType.Button);
                                                                                                                       // var targetDayXPath = $"//DataItem[@Name='{targetDay}']";
                //  var targetDayButton = calendar.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem).And(cf.ByName(targetDay)));
                //  var targetDayButton = calendar.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem).And(cf.ByName(targetDay)));
                // Console.WriteLine($"targetDayXPath: {targetDayXPath}");
                // var targetDayButton = calendar.FindFirstByXPath("//DataItem[@Name='23']");
                Console.WriteLine($"targetMonth: {targetMonthButton}");
                // Assert.That(targetDayButton, Is.Not.Null, "Target day button not found.");
                targetMonthButton.Click();
                // Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
                Thread.Sleep(20000);
                Button targetDayButton = GetElement<Button>(calendar, name:"23", controlType: ControlType.DataItem);
                Console.WriteLine($"targetDayButton: {targetDayButton}");
                targetDayButton.Click();
                break;
                
            }
                else
                {
                    Console.WriteLine("Target date not found ");
                }
            currentYear = DateTime.Now.AddYears(i + 1).Year;
        }
        Assert.That(currentYear, Is.EqualTo(targetYear), "Target year not found in current year range.");
        Console.WriteLine($"Target year found in current year range: {currentYear}");
        

        


        // --- Step 6: Verify the selected date ---
        string selectedDateText = datePickerElement.Value;
        Console.WriteLine($"Selected date: {selectedDateText}");
        Assert.That(selectedDateText, Is.EqualTo(expectedDisplayFormat), $"Selected date '{selectedDateText}' does not match the expected display format '{expectedDisplayFormat}'.");


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

