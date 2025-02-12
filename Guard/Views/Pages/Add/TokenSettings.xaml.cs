﻿using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Guard.Core;
using Guard.Core.Icons;
using Guard.Core.Import;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Views.UIComponents;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace Guard.Views.Pages.Add
{
    /// <summary>
    /// Interaktionslogik für TokenDetails.xaml
    /// </summary>
    public partial class TokenSettings : Page
    {
        private IconManager.TotpIcon? selectedIcon;
        private readonly IconManager.TotpIcon defaultIcon;
        private readonly MainWindow mainWindow;
        private readonly string action;
        private readonly TOTPTokenHelper? existingToken;

        public TokenSettings()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            action =
                NavigationContextManager.CurrentContext["action"] as string
                ?? throw new System.Exception("No action specified for TokenSettings");

            if (action != "add" && action != "edit")
            {
                throw new System.Exception("Invalid action specified for TokenSettings");
            }

            if (action.Equals("edit"))
            {
                int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
                existingToken = TokenManager.GetTokenById(tokenID);
            }

            NavigationContextManager.ClearContext();

            defaultIcon = IconManager.GetIcon("default", IconManager.IconType.Default);
            IconSvgView.SvgSource = defaultIcon.Svg;
            Issuer.OriginalItemsSource = IconManager.GetIconNames();

            foreach (string algorithm in new[] { "SHA1", "SHA256", "SHA512" })
            {
                AlgorithmComboBox.Items.Add(
                    new ComboBoxItem { Content = algorithm, Tag = algorithm }
                );
            }
            AlgorithmComboBox.SelectedItem = AlgorithmComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == "SHA1");

            // Set values if action is edit
            if (existingToken != null)
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                Issuer.Text = existingToken.dBToken.Issuer;
                Secret.Password = encryptionHelper.DecryptBytesToString(
                    existingToken.dBToken.EncryptedSecret
                );
                if (existingToken.dBToken.Icon != null)
                {
                    selectedIcon = IconManager.GetIcon(
                        existingToken.dBToken.Icon,
                        existingToken.dBToken.IconType ?? IconManager.IconType.Any
                    );

                    NoIconText.Visibility = Visibility.Collapsed;

                    if (selectedIcon.Type == IconManager.IconType.Custom)
                    {
                        showCustomImage();
                    }
                    else
                    {
                        IconSvgView.SvgSource = selectedIcon.Svg;
                        ImageLicense.Text = IconManager.GetLicense(selectedIcon);
                    }
                }
                if (existingToken.dBToken.EncryptedUsername != null)
                {
                    Username.Text = encryptionHelper.DecryptBytesToString(
                        existingToken.dBToken.EncryptedUsername
                    );
                }
                if (existingToken.dBToken.EncryptedNotes != null)
                {
                    try
                    {
                        MemoryStream notesStream =
                            new(
                                encryptionHelper.DecryptBytes(existingToken.dBToken.EncryptedNotes)
                            );
                        TextRange notesRange =
                            new(Notes.Document.ContentStart, Notes.Document.ContentEnd);
                        notesRange.Load(notesStream, DataFormats.Xaml);
                        notesStream.Close();
                    }
                    catch (Exception ex)
                    {
                        ShowEror(ex.Message);
                    }
                }
                if (existingToken.dBToken.Algorithm != null)
                {
                    AlgorithmComboBox.SelectedItem = AlgorithmComboBox
                        .Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(x =>
                            (string)x.Tag == existingToken.dBToken.Algorithm.ToString()
                        );
                }
                if (existingToken.dBToken.Digits != null)
                {
                    DigitsBox.Text = existingToken.dBToken.Digits.ToString();
                }
                if (existingToken.dBToken.Period != null)
                {
                    PeriodBox.Text = existingToken.dBToken.Period.ToString();
                }

                Loaded += (sender, e) =>
                {
                    Issuer.Text = existingToken.dBToken.Issuer;
                };
            }

            Issuer.SuggestionChosen += AutoSuggestBoxOnSuggestionChosen;
            Issuer.TextChanged += AutoSuggestBoxOnTextChanged;

            Loaded += (sender, e) =>
            {
                if (action.Equals("edit"))
                {
                    mainWindow.SetPageTitle(I18n.GetString("i.page.tokensettings.edit"));
                }
                else
                {
                    mainWindow.SetPageTitle(I18n.GetString("i.page.tokensettings.add"));
                }
            };
        }

        private void AutoSuggestBoxOnSuggestionChosen(
            AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args
        )
        {
            if (sender.IsSuggestionListOpen)
                return;

            if (args.SelectedItem is not string selectedSuggestBoxItem)
                return;

            if (selectedIcon != null && selectedIcon.Type == IconManager.IconType.Custom)
            {
                IconManager.RemoveCustomIcon(selectedIcon.Name);
            }

            ImageIconView.Source = null;
            ImageIconView.Visibility = Visibility.Collapsed;
            IconSvgView.Visibility = Visibility.Visible;
            selectedIcon = IconManager.GetIcon(selectedSuggestBoxItem, IconManager.IconType.Any);
            IconSvgView.Source = null;
            IconSvgView.SvgSource = selectedIcon.Svg;
            ImageLicense.Text = IconManager.GetLicense(selectedIcon);
            NoIconText.Visibility = Visibility.Collapsed;
        }

        private void AutoSuggestBoxOnTextChanged(
            AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args
        )
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            if (selectedIcon != null && selectedIcon.Type == IconManager.IconType.Custom)
            {
                return;
            }

            IconSvgView.SvgSource = defaultIcon.Svg;
            ImageLicense.Text = string.Empty;
            NoIconText.Visibility = Visibility.Visible;
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormattingDialog.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;

            if (string.IsNullOrWhiteSpace(Issuer.Text))
            {
                ShowEror(I18n.GetString("td.noissuer"));
                return;
            }
            // Validate secret
            if (string.IsNullOrWhiteSpace(Secret.Password))
            {
                ShowEror(I18n.GetString("td.nosecret"));
                return;
            }

            string normalizedSecret = OTPUriParser.NormalizeSecret(Secret.Password);
            if (!OTPUriParser.IsValidSecret(normalizedSecret))
            {
                ShowEror(I18n.GetString("td.invalidsecret"));
                return;
            }

            // Validate digits
            if (!int.TryParse(DigitsBox.Text, out int digits) || digits < 4 || digits > 9)
            {
                ShowEror(I18n.GetString("td.invaliddigits"));
                return;
            }
            // Validate period
            if (!int.TryParse(PeriodBox.Text, out int period) || period < 1 || period > 3600)
            {
                ShowEror(I18n.GetString("td.invalidperiod"));
                return;
            }

            SaveButton.IsEnabled = false;

            try
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                DBTOTPToken dbToken =
                    new()
                    {
                        Id =
                            existingToken != null
                                ? existingToken.dBToken.Id
                                : TokenManager.GetNextId(),
                        Issuer = Issuer.Text,
                        EncryptedSecret = encryptionHelper.EncryptStringToBytes(normalizedSecret),
                        CreationTime = DateTime.Now
                    };

                if (digits != 6)
                {
                    dbToken.Digits = digits;
                }
                TOTPAlgorithm algorithm = (TOTPAlgorithm)AlgorithmComboBox.SelectedIndex;
                if (algorithm != TOTPAlgorithm.SHA1)
                {
                    dbToken.Algorithm = algorithm;
                }
                if (period != 30)
                {
                    dbToken.Period = period;
                }

                if (selectedIcon != null)
                {
                    dbToken.Icon = selectedIcon.Name;
                    dbToken.IconType = selectedIcon.Type;
                }

                if (!string.IsNullOrWhiteSpace(Username.Text))
                {
                    dbToken.EncryptedUsername = encryptionHelper.EncryptStringToBytes(
                        Username.Text
                    );
                }
                else
                {
                    dbToken.EncryptedUsername = null;
                }

                TextRange notesTextRange =
                    new(Notes.Document.ContentStart, Notes.Document.ContentEnd);

                if (
                    !string.IsNullOrWhiteSpace(notesTextRange.Text)
                    && !notesTextRange.Text.Equals("\r\n")
                )
                {
                    MemoryStream notesStream = new();
                    notesTextRange.Save(notesStream, DataFormats.Xaml);
                    dbToken.EncryptedNotes = encryptionHelper.EncryptBytes(notesStream.ToArray());
                    notesStream.Close();
                }
                else
                {
                    dbToken.EncryptedNotes = null;
                }

                if (existingToken != null)
                {
                    TokenManager.DeleteTokenById(dbToken.Id);
                    dbToken.CreationTime = existingToken.dBToken.CreationTime;
                    dbToken.UpdatedTime = DateTime.Now;
                    TokenManager.AddToken(dbToken);
                    mainWindow.GetStatsClient()?.TrackEvent("TokenEdited");

                    NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                    NavigationContextManager.CurrentContext["type"] = "edited";
                    mainWindow.Navigate(typeof(TokenSuccessPage));
                    return;
                }

                dbToken.UpdatedTime = DateTime.Now;

                if (!TokenManager.AddToken(dbToken))
                {
                    throw new Exception(I18n.GetString("import.duplicate"));
                }

                mainWindow.GetStatsClient()?.TrackEvent("TokenAddedManually");

                NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                NavigationContextManager.CurrentContext["type"] = "added";
                mainWindow.Navigate(typeof(TokenSuccessPage));
            }
            catch (Exception ex)
            {
                ShowEror(ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowEror(string message)
        {
            InfoBar.Title = I18n.GetString("error");
            InfoBar.Message = message;
            InfoBar.Severity = InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }

        private void ExpertSettings_Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button button)
            {
                button.Visibility = Visibility.Collapsed;
            }

            ExpertWarningBar.IsOpen = false;
            ExpertWarningBar.Visibility = Visibility.Collapsed;
            AlgorithmComboBox.IsEnabled = true;
            DigitsBox.IsEnabled = true;
            PeriodBox.IsEnabled = true;
        }

        private async void CustomIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog =
                    new()
                    {
                        Filter = "Icon (*.jpg, *.jpeg, *.png, *.svg) | *.jpg; *.jpeg; *.png; *.svg",
                        Title = I18n.GetString("i.td.customicon.dialog.title")
                    };
                bool? result = openFileDialog.ShowDialog();
                if (result != true)
                {
                    return;
                }
                if (string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    return;
                }

                string name = await IconManager.AddCustomIcon(openFileDialog.FileName);

                ImageIconView.Source = null;
                IconSvgView.SvgSource = null;
                IconSvgView.Source = null;

                if (selectedIcon != null && selectedIcon.Type == IconManager.IconType.Custom)
                {
                    IconManager.RemoveCustomIcon(selectedIcon.Name);
                }

                selectedIcon = IconManager.GetIcon(name, IconManager.IconType.Custom);

                showCustomImage();
            }
            catch (Exception ex)
            {
                ShowEror(ex.Message);
            }
        }

        private void showCustomImage()
        {
            if (selectedIcon == null || selectedIcon.Path == null)
            {
                return;
            }
            if (selectedIcon.Path.AbsolutePath.EndsWith(".svg"))
            {
                ImageIconView.Visibility = Visibility.Collapsed;
                IconSvgView.Visibility = Visibility.Visible;
                IconSvgView.Source = selectedIcon.Path;
            }
            else
            {
                IconSvgView.Visibility = Visibility.Collapsed;
                ImageIconView.Visibility = Visibility.Visible;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = selectedIcon.Path;
                bitmap.EndInit();
                ImageIconView.Source = bitmap;
            }
            ImageLicense.Text = string.Empty;
            NoIconText.Visibility = Visibility.Collapsed;
        }
    }
}
