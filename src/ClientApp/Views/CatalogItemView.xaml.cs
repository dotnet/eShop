using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.ClientApp.Views;

public partial class CatalogItemView
{
    public CatalogItemView(CatalogItemViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

