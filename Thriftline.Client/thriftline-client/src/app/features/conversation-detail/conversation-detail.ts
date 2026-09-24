import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth';
import {
  ConversationService,
  ConversationDetailResponse,
  MessageResponse
} from '../../core/conversations/conversation';

interface PendingProduct {
  id: string;
  name: string;
  price: number;
  imageUrl: string | null;
}

@Component({
  selector: 'app-conversation-detail',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './conversation-detail.html',
  styleUrl: './conversation-detail.scss'
})
export class ConversationDetail implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private conversationService = inject(ConversationService);
  private authService = inject(AuthService);

  conversation = signal<ConversationDetailResponse | null>(null);
  // Diambil murni dari state navigasi (cuma ada kalau baru datang dari tombol
  // "Chat" di halaman produk) - bukan dari data server, supaya tidak ikut
  // kelihatan/ke-lampirkan di sisi orang lain yang buka percakapan yang sama.
  pendingProduct = signal<PendingProduct | null>(
    ((history.state as { attachProduct?: PendingProduct } | undefined)?.attachProduct) ?? null
  );
  isLoading = signal(true);
  newMessage = signal('');
  isSending = signal(false);

  private conversationId!: string;
  private pollHandle?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.conversationId = this.route.snapshot.paramMap.get('id')!;
    this.load();
    this.pollHandle = setInterval(() => this.load(true), 4000);
  }

  ngOnDestroy(): void {
    if (this.pollHandle) {
      clearInterval(this.pollHandle);
    }
  }

  load(silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }

    this.conversationService.getById(this.conversationId).subscribe({
      next: (data) => {
        this.conversation.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  dismissPendingProduct(): void {
    this.pendingProduct.set(null);
  }

  isMine(message: MessageResponse): boolean {
    return message.senderId === this.authService.currentUser()?.userId;
  }

  formatTime(date: string): string {
    return new Intl.DateTimeFormat('id-ID', { timeStyle: 'short' }).format(new Date(date));
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency',
      currency: 'IDR',
      maximumFractionDigits: 0
    }).format(price);
  }

  send(): void {
    const content = this.newMessage().trim();
    if (!content || this.isSending()) return;

    this.isSending.set(true);
    const attachedProductId = this.pendingProduct()?.id;

    this.conversationService.sendMessage(this.conversationId, content, attachedProductId).subscribe({
      next: () => {
        this.newMessage.set('');
        this.isSending.set(false);
        this.pendingProduct.set(null);
        this.load(true);
      },
      error: () => this.isSending.set(false)
    });
  }
}
