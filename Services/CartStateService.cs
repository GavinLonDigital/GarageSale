namespace GarageSale.Services;

public class CartStateService
{
    public int ItemCount { get; private set; }

    public event Action? OnCartChanged;

    public void SetCount(int count)
    {
        ItemCount = count;
        OnCartChanged?.Invoke();
    }

    public void Increment()
    {
        ItemCount++;
        OnCartChanged?.Invoke();
    }

    public void Decrement()
    {
        if (ItemCount > 0)
            ItemCount--;
        OnCartChanged?.Invoke();
    }
}
