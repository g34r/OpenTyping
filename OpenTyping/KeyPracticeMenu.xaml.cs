﻿using OpenTyping.Resources.Lang;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace OpenTyping
{
    /// <summary>
    /// KeyPracticeMenu.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class KeyPracticeMenu : UserControl
    {
        public bool NoShiftMode { get; set; }

        public KeyPracticeMenu()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            List<KeyPos> pressedKeys = KeyLayoutBox.PressedKeys();
            
            if (pressedKeys.Count <= 1)
            {
                MessageBox.Show(LangStr.ErrMsg1,
                                LangStr.AppName,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            var keyPracticeWindow = new KeyPracticeWindow(pressedKeys, NoShiftMode);
            keyPracticeWindow.ShowDialog();
        }
    }
}
