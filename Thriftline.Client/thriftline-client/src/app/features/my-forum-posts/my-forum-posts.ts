import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { ForumService, ForumPostSummaryResponse } from '../../core/forum/forum';
import { MEDIA_TYPE_VIDEO } from '../../core/uploads/upload';
import { AppHeader } from '../../shared/app-header/app-header';

@Component({
  selector: 'app-my-forum-posts',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatMenuModule, AppHeader],
  templateUrl: './my-forum-posts.html',
  styleUrl: './my-forum-posts.scss'
})
export class MyForumPosts implements OnInit {
  private forumService = inject(ForumService);

  posts = signal<ForumPostSummaryResponse[]>([]);
  isLoading = signal(true);

  readonly MEDIA_TYPE_VIDEO = MEDIA_TYPE_VIDEO;
  private readonly avatarColors = ['#4f6bff', '#22c55e', '#f59e0b', '#a855f7', '#ef4444', '#0ea5e9'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.forumService.getMine().subscribe({
      next: (data) => {
        this.posts.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleLike(event: Event, post: ForumPostSummaryResponse): void {
    event.stopPropagation();
    event.preventDefault();

    this.forumService.toggleLike(post.id).subscribe((result) => {
      this.posts.update((items) =>
        items.map((p) => (p.id === post.id ? { ...p, isLikedByMe: result.liked, likeCount: result.likeCount } : p))
      );
    });
  }

  deletePost(event: Event, id: string): void {
    event.stopPropagation();
    event.preventDefault();
    if (!confirm('Hapus diskusi ini?')) return;

    this.forumService.remove(id).subscribe(() => {
      this.posts.update((items) => items.filter((p) => p.id !== id));
    });
  }

  avatarColor(name: string): string {
    const sum = name.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return this.avatarColors[sum % this.avatarColors.length];
  }
}
