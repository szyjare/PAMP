using System.Windows.Data;
using System.Windows.Markup;

namespace PAMP
{
    public class LocExtension : MarkupExtension
    {
        private string _key;

        public LocExtension(string key)
        {
            _key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{_key}]")
            {
                Source = TranslationSource.Instance,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}