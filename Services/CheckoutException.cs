namespace GarageSale.Services;

// Only messages from this type are safe to show to the buyer.
public class CheckoutException(string message) : Exception(message);
