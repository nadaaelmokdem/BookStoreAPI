using BookStoreApi.Dtos.Orders;
using FluentValidation;

namespace BookStoreApi.Validators;

public class OrderItemRequestDtoValidator : AbstractValidator<OrderItemRequestDto>
{
    public OrderItemRequestDtoValidator()
    {
        RuleFor(x => x.BookId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("An order must contain at least one item.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestDtoValidator());
    }
}
