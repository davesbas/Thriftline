import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router, RouterOutlet, ActivatedRoute, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { filter } from 'rxjs';
import { AuthService } from '../../core/auth/auth';
import { ConversationService, ConversationSummaryResponse } from '../../core/conversations/conversation';
import { AppHeader } from '../../shared/app-header/app-header';

const AVATAR_COLORS = ['#4f6bff', '#22c55e', '#f59e0b', '#a855f7', '#ef4444', '#0ea5e9', '#ec4899'];

@Component({
  selector: 'app-conversations',
  standalone: true,
  imports: [RouterOutlet, FormsModule, MatIconModule, AppHeader],
  templateUrl: './conversations.html',
  styleUrl: './conversations.scss'
})
export class Conversations implements OnInit {
  private conversationService = inject(ConversationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  conversations = signal<ConversationSummaryResponse[]>([]);
  isLoading = signal(true);
  activeId = signal<string | null>(null);
  searchTerm = signal('');

  filteredConversations = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.conversations();

    return this.conversations().filter((c) => this.otherPartyName(c).toLowerCase().includes(term));
  });

  ngOnInit(): void {
    this.loadList();

    this.activeId.set(this.route.firstChild?.snapshot.paramMap.get('id') ?? null);
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.activeId.set(this.route.firstChild?.snapshot.paramMap.get('id') ?? null);
    });
  }

  loadList(): void {
    this.conversationService.getMyConversations().subscribe({
      next: (data) => {
        this.conversations.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  otherPartyName(conversation: ConversationSummaryResponse): string {
    const userId = this.authService.currentUser()?.userId;
    return conversation.buyerId === userId ? conversation.storeName : conversation.buyerName;
  }

  avatarColor(conversation: ConversationSummaryResponse): string {
    const name = this.otherPartyName(conversation);
    let sum = 0;
    for (let i = 0; i < name.length; i++) {
      sum += name.charCodeAt(i);
    }
    return AVATAR_COLORS[sum % AVATAR_COLORS.length];
  }

  formatTime(date: string | null): string {
    if (!date) return '';
    return new Intl.DateTimeFormat('id-ID', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(date));
  }

  openConversation(conversation: ConversationSummaryResponse): void {
    this.router.navigate(['/conversations', conversation.id]);
  }
}
