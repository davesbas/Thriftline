import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home').then((m) => m.Home) },
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then((m) => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register').then((m) => m.Register) },
  {
    path: 'product/:id',
    loadComponent: () => import('./features/product-detail/product-detail').then((m) => m.ProductDetail)
  },
  {
    path: 'cart',
    canActivate: [authGuard],
    loadComponent: () => import('./features/cart/cart').then((m) => m.Cart)
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/checkout/checkout').then((m) => m.Checkout)
  },
  {
    path: 'payment/batch',
    canActivate: [authGuard],
    loadComponent: () => import('./features/payment-batch/payment-batch').then((m) => m.PaymentBatch)
  },
  {
    path: 'payment/:orderId',
    canActivate: [authGuard],
    loadComponent: () => import('./features/payment/payment').then((m) => m.Payment)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/orders').then((m) => m.Orders)
  },
    {
    path: 'store',
    canActivate: [authGuard],
    loadComponent: () => import('./features/store-dashboard/store-dashboard').then((m) => m.StoreDashboard)
  },
  {
    path: 'store/products/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/product-form/product-form').then((m) => m.ProductForm)
  },
  {
    path: 'store/products/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/product-form/product-form').then((m) => m.ProductForm)
  },
  {
    path: 'notifications',
    canActivate: [authGuard],
    loadComponent: () => import('./features/notifications/notifications').then((m) => m.Notifications)
  },
  {
    path: 'conversations',
    canActivate: [authGuard],
    loadComponent: () => import('./features/conversations/conversations').then((m) => m.Conversations),
    children: [
      {
        path: ':id',
        loadComponent: () =>
          import('./features/conversation-detail/conversation-detail').then((m) => m.ConversationDetail)
      }
    ]
  },
  {
    path: 'forum',
    loadComponent: () => import('./features/forum/forum').then((m) => m.Forum)
  },
  {
    path: 'forum/:id',
    loadComponent: () => import('./features/forum-post/forum-post').then((m) => m.ForumPost)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile').then((m) => m.Profile)
  },
  {
    path: 'profile/settings',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile-settings/profile-settings').then((m) => m.ProfileSettings)
  },
  {
    path: 'profile/posts',
    canActivate: [authGuard],
    loadComponent: () => import('./features/my-forum-posts/my-forum-posts').then((m) => m.MyForumPosts)
  },
  {
    path: 'wishlist',
    canActivate: [authGuard],
    loadComponent: () => import('./features/wishlist/wishlist').then((m) => m.Wishlist)
  },
  {
    path: 'store/orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/store-orders/store-orders').then((m) => m.StoreOrders)
  },
  {
    path: 'store/:id',
    loadComponent: () => import('./features/store-profile/store-profile').then((m) => m.StoreProfile)
  },
  {
    path: 'reviews/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/write-review/write-review').then((m) => m.WriteReview)
  },
  {
    path: 'reviews/:orderItemId',
    canActivate: [authGuard],
    loadComponent: () => import('./features/review-detail/review-detail').then((m) => m.ReviewDetail)
  },
  {
    path: 'profile/addresses',
    canActivate: [authGuard],
    loadComponent: () => import('./features/addresses/addresses').then((m) => m.Addresses)
  },
  {
    path: 'wallet',
    canActivate: [authGuard],
    loadComponent: () => import('./features/wallet/wallet').then((m) => m.Wallet)
  }
];
