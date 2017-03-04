// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using NorthwindPhoto.Model;
using Windows.Foundation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace NorthwindPhoto
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Gallery : Page
    {
        private int _animationDuration = 400;
        private Compositor _compositor;
        private Visual _gridview;

        private SpotLight spotLight;
        private float spotlight_z = 300;
        private AmbientLight ambientLight;
        private Color ambientLightColorOn = Color.FromArgb(255, 220, 220, 220);
        private Color ambientLightColorOff = Color.FromArgb(255, 255, 255, 255);

        private float durationHover = 500;
        private float shadowDistance = 5;

        public Gallery()
        {
            InitializeComponent();
        }

        /// <summary>
        /// TODO: 1.Show/Hide
        /// When the image is being loaded into the container a show/hide animation is being applied
        /// </summary>
        private void PhotoCollectionViewer_ChoosingItemContainer(ListViewBase sender,
            ChoosingItemContainerEventArgs args)
        {
            bool isInCollection = false;
            args.ItemContainer = args.ItemContainer ?? new GridViewItem();

            // Set up show and hide animations for this item container.
            // Need to handle ChoosingItemContainer to set up animations before the element is added to the tree.

            // Setup Show animations. This should only play when 
            // the app is loaded first or when coming back here from collage page

            ElementCompositionPreview.SetImplicitShowAnimation(args.ItemContainer, SetupDefaultShowAnimation(args.ItemIndex));
            ElementCompositionPreview.SetImplicitHideAnimation(args.ItemContainer, SetupDefaultHideAnimation(args.ItemIndex));

            //Target each item that is added to the gridView
            //TODO: Untarget when an item is unloaded.

            foreach (Visual v in spotLight.Targets)
            {
                if (v == ElementCompositionPreview.GetElementVisual(args.ItemContainer))
                {
                    isInCollection = true;
                    break;
                }
            }

            if (!isInCollection)
            {
                spotLight.Targets.Add(ElementCompositionPreview.GetElementVisual(args.ItemContainer));
                ambientLight.Targets.Add(ElementCompositionPreview.GetElementVisual(args.ItemContainer));
            }

        }

        #region Pointer Entered / Exit

        public ObservableCollection<Photo> Photos { get; set; } = App.PhotoCollection;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this)?.Compositor;

            if (e.Parameter != null)
                int.TryParse(e.Parameter.ToString(), out _animationDuration);

            _gridview = ElementCompositionPreview.GetElementVisual(this);
            ApplyLighting();
        }

        private void PhotoCollectionViewer_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.MainFrame.Navigate(typeof(ImageEditingPage), e.ClickedItem as Photo);
        }

        private void GalleryItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var element = ElementCompositionPreview.GetElementVisual((UIElement)sender);

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));

            element.CenterPoint = new Vector3(275f / 2, 275f / 2, 275f / 2);
            element.StartAnimation("Scale", scaleAnimation);

            var shadowBorder = (Grid)sender;
            ElementCompositionPreview.SetElementChildVisual((UIElement)shadowBorder.FindName("Shadow"), null);

            // Update AmbientLight when the item is no longer hovered on

            var ambientLightColorAnimation = _compositor.CreateColorKeyFrameAnimation();
            ambientLightColorAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            ambientLightColorAnimation.InsertKeyFrame(1f, ambientLightColorOff);
            ambientLight.StartAnimation(nameof(AmbientLight.Color), ambientLightColorAnimation);
        }

        private void GalleryItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;

            //Setup Animations for the current Image Visual

            var element = ElementCompositionPreview.GetElementVisual(frameworkElement);
            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1.1f, 1.1f, 1.1f));

            element.CenterPoint = new Vector3(275f / 2, 275f / 2, 275f / 2);
            element.StartAnimation("Scale", scaleAnimation);

            var shadowBorder = (Grid)sender;
            var shadow = _compositor.CreateDropShadow();
            shadow.Color = Colors.DimGray;

            var shadowBlurAnimation = _compositor.CreateScalarKeyFrameAnimation();
            shadowBlurAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            shadowBlurAnimation.InsertKeyFrame(0f, 1f);
            shadowBlurAnimation.InsertKeyFrame(1f, 18f);
            shadow.StartAnimation(nameof(shadow.BlurRadius), shadowBlurAnimation);

            var shadowOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            shadowOffsetAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            shadowOffsetAnimation.InsertKeyFrame(1f, new Vector3(shadowDistance, shadowDistance, 0));
            shadow.StartAnimation(nameof(Visual.Offset), shadowOffsetAnimation);

            var sprite = _compositor.CreateSpriteVisual();
            sprite.Size = new System.Numerics.Vector2((float)shadowBorder.ActualWidth, (float)shadowBorder.ActualHeight);
            sprite.Shadow = shadow;

            ElementCompositionPreview.SetElementChildVisual((UIElement)shadowBorder.FindName("Shadow"), sprite);

            //Move the spotlight to the center of the hovered light

            var ttv = frameworkElement.TransformToVisual(galleryPage);
            Point screenCoords = ttv.TransformPoint(new Point(0, 0));

            var mouse_x = screenCoords.X + frameworkElement.ActualWidth / 2;
            var mouse_y = screenCoords.Y + frameworkElement.ActualHeight / 2;


            var elementOffset = new Vector3((float)mouse_x, (float)mouse_y, spotlight_z); // EF: Need to calculate tile offset here
            var spotlightOffsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            spotlightOffsetAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            spotlightOffsetAnimation.InsertKeyFrame(1f, elementOffset);
            spotLight.StartAnimation(nameof(Visual.Offset), spotlightOffsetAnimation);

            var ambientLightColorAnimation = _compositor.CreateColorKeyFrameAnimation();
            ambientLightColorAnimation.Duration = TimeSpan.FromMilliseconds(durationHover);
            ambientLightColorAnimation.InsertKeyFrame(1f, ambientLightColorOn);
            ambientLight.StartAnimation(nameof(AmbientLight.Color), ambientLightColorAnimation);
        }

        private void ScaleImage(FrameworkElement frameworkElement, float scaleAmout)
        {
            var element = ElementCompositionPreview.GetElementVisual(frameworkElement);

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);
            scaleAnimation.InsertKeyFrame(1f, new Vector3(scaleAmout, scaleAmout, scaleAmout));

            element.CenterPoint = new Vector3((float) frameworkElement.ActualHeight / 2,
                (float) frameworkElement.ActualWidth / 2, 275f / 2);
            element.StartAnimation("Scale", scaleAnimation);

            if (scaleAmout > 1)
            {
                var shadow = _compositor.CreateDropShadow();
                shadow.Offset = new Vector3(15, 15, -10);
                shadow.BlurRadius = 5;
                shadow.Color = Colors.DarkGray;

                var sprite = _compositor.CreateSpriteVisual();
                sprite.Size = new Vector2((float) frameworkElement.ActualWidth - 20,
                    (float) frameworkElement.ActualHeight - 20);
                sprite.Shadow = shadow;

                ElementCompositionPreview.SetElementChildVisual((UIElement) frameworkElement.FindName("Shadow"), sprite);
            }
            else
            {
                ElementCompositionPreview.SetElementChildVisual((UIElement) frameworkElement.FindName("Shadow"), null);
            }
        }

        #endregion

        #region lighting

        private void ApplyLighting()
        {
            ambientLight = _compositor.CreateAmbientLight();
            ambientLight.Color = ambientLightColorOff;

            spotLight = _compositor.CreateSpotLight();
            spotLight.CoordinateSpace = _gridview;
            spotLight.Offset = new Vector3(0, 0, spotlight_z);
            spotLight.InnerConeColor = Colors.Yellow;
            spotLight.InnerConeAngleInDegrees = 0.0f;
            spotLight.OuterConeColor = Colors.DodgerBlue;
            spotLight.OuterConeAngleInDegrees = 45.0f;
        }

        private void PhotoCollectionViewer_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            spotLight.Offset = new Vector3(5000, 5000, spotlight_z);

        }
        #endregion

        #region showHideAnimations
        private CompositionAnimationGroup SetupDefaultShowAnimation(int index)
        {
            // Set up show and hide animations for this item container.

            // Need to handle ChoosingItemContainer to set up animations before the element is added to the tree.

            var fadeIn = _compositor.CreateScalarKeyFrameAnimation();
            fadeIn.Target = "Opacity";
            fadeIn.Duration = TimeSpan.FromMilliseconds(_animationDuration);
            fadeIn.InsertKeyFrame(0, 0);
            fadeIn.InsertKeyFrame(1, 1);
            fadeIn.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

            // Add a staggering delay factor to the entrance
            fadeIn.DelayTime = TimeSpan.FromMilliseconds(_animationDuration * 0.125 * index);
            var scaleIn = _compositor.CreateVector3KeyFrameAnimation();
            scaleIn.Target = "Scale";
            scaleIn.Duration = TimeSpan.FromMilliseconds(_animationDuration);
            scaleIn.InsertKeyFrame(0f, new System.Numerics.Vector3(1.2f, 1.2f, 1.2f));
            scaleIn.InsertKeyFrame(1f, new System.Numerics.Vector3(1f, 1f, 1f));
            scaleIn.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            scaleIn.DelayTime = TimeSpan.FromMilliseconds(_animationDuration * 0.125 * index);

            var animationFadeInGroup = _compositor.CreateAnimationGroup();
            animationFadeInGroup.Add(fadeIn);
            animationFadeInGroup.Add(scaleIn);
            return animationFadeInGroup;
        }

        private CompositionAnimationGroup SetupDefaultHideAnimation(int index)
        {
            ScalarKeyFrameAnimation fadeOut = _compositor.CreateScalarKeyFrameAnimation();
            fadeOut.Target = "Opacity";
            fadeOut.Duration = TimeSpan.FromMilliseconds(_animationDuration);
            fadeOut.InsertKeyFrame(1, 0);

            var hideAnimation = _compositor.CreateAnimationGroup();
            hideAnimation.Add(fadeOut);
            return hideAnimation;
        }
        #endregion
    }
}