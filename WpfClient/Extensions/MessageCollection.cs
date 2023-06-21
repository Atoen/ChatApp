using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WpfClient.Models;

namespace WpfClient.Extensions;

public delegate void MessageAddedHandler(MessageCollection sender, Message obj);

public delegate void PageAddedHandler(MessageCollection sender, IList<Message> page);


public class MessageCollection : ObservableCollection<Message>
{
    public event MessageAddedHandler? MessageAdded;
    public event PageAddedHandler? PageAdded;
    public event PageAddedHandler? PageAdding;

    public new void Add(Message message)
    {
        base.Add(message);
        MessageAdded?.Invoke(this, message);
    }

    public void InsertPage(IList<Message> page, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (index < 0 || index > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (page.Count == 0) return;

        CheckReentrancy();
        
        PageAdding?.Invoke(this, page);

        for (var i = page.Count - 1; i >= 0; i--)
        {
            Items.Insert(index, page[i]);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        PageAdded?.Invoke(this, page);
    }
}