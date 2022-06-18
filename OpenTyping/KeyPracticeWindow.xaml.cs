﻿using MahApps.Metro.Controls;
using OpenTyping.Properties;
using OpenTyping.Resources.Lang;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Media;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace OpenTyping
{
    /// <summary>
    /// KeyPracticeWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class KeyPracticeWindow : MetroWindow, INotifyPropertyChanged
    {
        public class KeyInfo
        {
            public KeyInfo(string keyData, KeyPos pos, bool isShift)
            {
                KeyData = keyData;
                Pos = pos;
                IsShift = isShift;
            }

            public string KeyData { get; set; }
            public KeyPos Pos { get; set; }
            public bool IsShift { get; set; }
        }

        private readonly IList<KeyPos> keyList;
        private readonly bool noShiftMode;
        private readonly Dictionary<KeyPos, int> incorrectStats = new Dictionary<KeyPos, int>();
        private bool isHandPopup;
        private bool isColored;

        private KeyInfo previousKey;
        public KeyInfo PreviousKey
        {
            get => previousKey;
            private set => SetField(ref previousKey, value);
        }

        private KeyInfo currentKey;
        public KeyInfo CurrentKey
        {
            get => currentKey;
            private set => SetField(ref currentKey, value);
        }

        private KeyInfo nextKey;
        public KeyInfo NextKey
        {
            get => nextKey;
            private set => SetField(ref nextKey, value);
        }

        private int correctCount = 0;
        public int CorrectCount
        {
            get => correctCount;
            private set => SetField(ref correctCount, value);
        }

        private int incorrectCount = 0;
        public int IncorrectCount
        {
            get => incorrectCount;
            private set => SetField(ref incorrectCount, value);
        }

        private static readonly Random Randomizer = new Random();
        private static readonly ThicknessAnimationUsingKeyFrames ShakeAnimation = new ThicknessAnimationUsingKeyFrames();

        private readonly MediaPlayer playMedia = new MediaPlayer();
        private readonly SoundPlayer playSound = new SoundPlayer(Properties.Resources.Pressed);
        private readonly Uri uri = new Uri("pack://siteoforigin:,,,/Resources/Sounds/WrongPressed.mp3");
        private readonly Volume volume;

        // Magnify window
        private bool isMagnified;
        private double baseFontSize;
        public double BaseFontSize
        {
            get => baseFontSize;
            private set => SetField(ref baseFontSize, value);
        }

        public KeyPracticeWindow(IList<KeyPos> keyList, bool noShiftMode)
        {
            BaseFontSize = App.BaseFontSize;

            InitializeComponent();
            this.SetTextBylanguage();
            this.FontAssignByLang();

            this.volume = (Volume)Settings.Default["Volume"];

            this.keyList = keyList;
            this.noShiftMode = noShiftMode;

            NextKey = RandomKey();
            var dispatcherOp = Dispatcher.BeginInvoke(DispatcherPriority.Loaded, 
                                   new Action(MoveKey));
            dispatcherOp.Completed += (s, e) => // When asynchronous completed
            {
                ColorToggleBtn.IsChecked = true; // Default is the Colored keys
                HandToggleBtn.IsChecked = true;  // Default is with hand guidance 
            };

            PreviewKeyDown += KeyPracticeWindow_PreviewKeyDown;

            double shakiness = 30;
            const double shakeDiff = 3;
            var keyFrames = new ThicknessKeyFrameCollection();

            for (int timeSpan = 5; shakiness > 0;)
            {
                keyFrames.Add(new EasingThicknessKeyFrame(new Thickness(0, 10, 0, 0))
                {
                    KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, timeSpan))
                });
                timeSpan += 5;

                keyFrames.Add(new EasingThicknessKeyFrame(new Thickness(shakiness, 10, 0, 0))
                {
                    KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, timeSpan))
                });
                timeSpan += 5;

                keyFrames.Add(new EasingThicknessKeyFrame(new Thickness(0, 10, 0, 0))
                {
                    KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, timeSpan))
                });
                timeSpan += 5;

                keyFrames.Add(new EasingThicknessKeyFrame(new Thickness(-shakiness, 10, 0, 0))
                {
                    KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, timeSpan))
                });
                timeSpan += 5;

                keyFrames.Add(new EasingThicknessKeyFrame(new Thickness(0, 10, 0, 0))
                {
                    KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, timeSpan))
                });
                timeSpan += 5;

                shakiness -= shakeDiff;
            }

            ShakeAnimation.KeyFrames = keyFrames;
        }

        private void SetTextBylanguage()
        {
            SelfWindow.Title = LangStr.AppName;
        }

        private void FontAssignByLang()
        {
            if ((string)Settings.Default["KeyLayout"] == "Lotincha")
            {
                PreviousTextBlock.FontFamily = new FontFamily("Times New Roman");
                CurrentTextBlock.FontFamily = new FontFamily("Times New Roman");
                NextTextBlock.FontFamily = new FontFamily("Times New Roman");
            }
        }

        private KeyInfo RandomKey()
        {
            KeyPos keyPos = keyList[Randomizer.Next(0, keyList.Count)];
            Key key = MainWindow.CurrentKeyLayout[keyPos];

            if (noShiftMode || string.IsNullOrEmpty(key.ShiftKeyData))
            {
                return new KeyInfo(key.KeyData, keyPos, false);
            }

            bool isShift = Randomizer.Next(0, 2) == 0;
            return new KeyInfo(isShift ? key.ShiftKeyData : key.KeyData, keyPos, isShift);
        }

        private void MoveKey()
        {
            PreviousKey = CurrentKey;
            if (PreviousKey != null)
            {
                KeyLayoutBox.ReleaseKey(PreviousKey.Pos, isColored);
                if (PreviousKey.IsShift)
                {
                    KeyLayoutBox.LShiftKey.Release();
                    KeyLayoutBox.RShiftKey.Release();
                }
            }

            CurrentKey = NextKey;
            KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, this.isHandPopup);
            if (CurrentKey.IsShift)
            {
                KeyLayoutBox.LShiftKey.PressCorrect();
                KeyLayoutBox.RShiftKey.PressCorrect();
            }

            NextKey = RandomKey();
        }

        private void KeyPracticeWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat) return;

            KeyPos pos = KeyPos.FromKeyCode(e.Key == System.Windows.Input.Key.ImeProcessed ? e.ImeProcessedKey : e.Key);

            if (e.Key == System.Windows.Input.Key.LeftShift || 
                e.Key == System.Windows.Input.Key.RightShift || 
                pos == null) return;

            bool isLShift = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift);
            bool isRShift = Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift);
            bool isShift = isLShift || isRShift;
            
            if (CurrentKey.Pos == pos && CurrentKey.IsShift == isShift)
            {
                if (this.volume == Volume.Up)
                {
                    playSound.Play();
                }
                CorrectCount++;
                MoveKey();
            }
            else // Wrong pressed
            {
                playMedia.Open(uri);
                if (this.volume != Volume.Off)
                {
                    playMedia.Play();
                }

                IncorrectCount++;

                if (!incorrectStats.ContainsKey(CurrentKey.Pos)) incorrectStats[CurrentKey.Pos] = 1;
                else incorrectStats[CurrentKey.Pos]++;

                KeyGrid.BeginAnimation(MarginProperty, ShakeAnimation);
                Dispatcher.Invoke(async () =>
                {
                    KeyLayoutBox.PressIncorrectKey(pos);
                    if (isLShift) KeyLayoutBox.LShiftKey.PressIncorrect();
                    if (isRShift) KeyLayoutBox.RShiftKey.PressIncorrect();

                    await Task.Delay(500);

                    if (CurrentKey.Pos == pos)
                    {
                        KeyLayoutBox.PressCorrectKey(pos, this.isHandPopup);
                    }
                    else
                    {
                        KeyLayoutBox.ReleaseKey(pos, isColored);
                    }

                    if (CurrentKey.IsShift) // 현재 키가 윗글쇠일 경우
                    {
                        if (isLShift) KeyLayoutBox.LShiftKey.PressCorrect();
                        if (isRShift) KeyLayoutBox.RShiftKey.PressCorrect();
                        // 원래 색(초록)으로 복구
                    }
                    else
                    {
                        if (isLShift) KeyLayoutBox.LShiftKey.Release();
                        if (isRShift) KeyLayoutBox.RShiftKey.Release();
                        // 현재 키가 윗글쇠가 아닌데 윗글쇠를 눌렀을 경우 누르기 취소
                    }
                });
            }
        }

        private void KeyPracticeWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.CurrentKeyLayout.Stats.AddStats(new KeyLayoutStats()
            {
                KeyIncorrectCount = incorrectStats
            });

            // Restore magnification
            if (isMagnified) BaseFontSize /= 1.5;
        }

        private void KeyPracticeWindow_Activated(object sender, EventArgs e)
        {
            if (CurrentKey != null)
            {
                KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, this.isHandPopup);
            }
        }

        private void KeyPracticeWindow_Deactivated(object sender, EventArgs e)
        {
            KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, false);
        }

        private void KeyPracticeWindow_LocationChanged(object sender, EventArgs e)
        {
            if (CurrentKey != null)
            {
                KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, this.isHandPopup);
            }
        }

        private void HandToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                if (toggleButton.IsChecked == true)
                {
                    this.isHandPopup = true;
                    if (KeyLayoutBox != null)
                    {
                        KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, true);
                    }
                }
                else
                {
                    this.isHandPopup = false;
                    if (KeyLayoutBox != null)
                    {
                        KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, false);
                    }
                }
            }
        }

        private void ColorToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                if (toggleButton.IsChecked == true)
                {
                    isColored = true;
                    if (KeyLayoutBox != null)
                    {
                        KeyLayoutBox.ToggleColoredKeys(true, CurrentKey.Pos);
                    }
                }
                else
                {
                    isColored = false;
                    if (KeyLayoutBox != null)
                    {
                        KeyLayoutBox.ToggleColoredKeys(false, CurrentKey.Pos);
                    }
                }
            }
        }

        // Pevent toggle on/off by spacebar
        private void ToggleButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Space)
            {
                e.Handled = true;
            }
        }

        private void MagnifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isMagnified)
            {
                SizeToContent = SizeToContent.Manual;
                Width = ActualWidth * 1.5;

                BaseFontSize *= 1.5;
                KeyLayoutBox.WidthRatio = 1.2;
                MagnifyIcon.Kind = MahApps.Metro.IconPacks.PackIconModernKind.MagnifyMinus;
                SizeToContent = SizeToContent.Height; // Have to call to fit to content's height again

                isMagnified = true;
            }
            else
            {
                SizeToContent = SizeToContent.WidthAndHeight;

                BaseFontSize /= 1.5;
                KeyLayoutBox.WidthRatio = 1.0;
                MagnifyIcon.Kind = MahApps.Metro.IconPacks.PackIconModernKind.MagnifyAdd;

                isMagnified = false;
            }

            KeyLayoutBox.LoadKeyLayout(); // Refresh key layout control

            // Repaint the colored key
            if (ColorToggleBtn.IsChecked == true)
            {
                KeyLayoutBox.ToggleColoredKeys(true, CurrentKey.Pos);
            }
            else
            {
                KeyLayoutBox.ToggleColoredKeys(false, CurrentKey.Pos);
            }

            KeyLayoutBox.PressCorrectKey(CurrentKey.Pos, this.isHandPopup); // Refresh current key
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
