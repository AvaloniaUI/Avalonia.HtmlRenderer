using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.HtmlRenderer
{
    static class PropertyHelper
    {

        /*public static AvaloniaProperty Register<TOwner, T>(string name, T def, Action<AvaloniaObject, AvaloniaPropertyChangedEventArgs> changed) where TOwner : AvaloniaObject
        {
            var pp = AvaloniaProperty.Register<TOwner, T>(name, def);
            Action<AvaloniaPropertyChangedEventArgs> cb = args =>
            {
                changed(args.Sender, args);
            };

            pp.Changed.Subscribe(cb);
            return pp;
        }



        */public static AvaloniaProperty Register<TOwner, T>(string name, T def, Action<IAvaloniaObject, AvaloniaPropertyChangedEventArgs> changed) where TOwner : IAvaloniaObject
        {
            var pp = AvaloniaProperty.Register<TOwner, T>(name, def);

            /*IObserver<AvaloniaPropertyChangedEventArgs> cb = new Observer<AvaloniaPropertyChangedEventArgs>.Create((args) =>
            {
                changed(args.Sender, args);
            });*/

            pp.Changed.Subscribe(args => 
            {
                changed.Invoke(args.Sender, args);
                /*if ((args != null) && (args.Sender != null) && (changed != null))
                    changed(args.Sender, args);*/
            });

            //pp.Changed.Subscribe(changed);
            return pp;
        }
    }
}
