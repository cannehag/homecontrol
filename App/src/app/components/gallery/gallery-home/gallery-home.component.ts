import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GalleryService, GalleryTreeNode } from '../gallery.service';

@Component({
  selector: 'home-gallery-home',
  templateUrl: './gallery-home.component.html',
  styleUrls: ['./gallery-home.component.less'],
})
export class GalleryHomeComponent implements OnInit {
  loading = true;
  connected = true; // optimistic until the status call resolves, to avoid a flash
  tree: GalleryTreeNode[] = [];
  breadcrumbs: GalleryTreeNode[] = [];
  currentChildren: GalleryTreeNode[] = [];

  authExpiresInDays: number | null = null;
  reauthResult: 'success' | 'error' | null = null;

  constructor(private galleryService: GalleryService, private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.queryParamMap.subscribe((params) => {
      const reauth = params.get('reauth');
      this.reauthResult = reauth === 'success' || reauth === 'error' ? reauth : null;
    });

    this.galleryService.getStatus().subscribe((status) => {
      this.connected = status.connected;
      this.authExpiresInDays = status.connected ? status.lightroomAuthExpiresInDays : null;

      if (status.connected) {
        this.loadTree();
      } else {
        this.loading = false;
      }
    });
  }

  renewLightroomLogin() {
    window.location.href = '/api/gallery/oauth/start';
  }

  private loadTree() {
    this.galleryService.getTree().subscribe({
      next: (tree) => {
        this.tree = tree;
        this.loading = false;
        this.route.paramMap.subscribe((params) => this.showFolder(params.get('folderId')));
      },
      error: () => {
        this.connected = false;
        this.loading = false;
      },
    });
  }

  private showFolder(folderId: string | null) {
    const path = folderId ? this.findPath(this.tree, folderId, []) : null;

    if (path) {
      this.breadcrumbs = path;
      this.currentChildren = path[path.length - 1].children;
    } else {
      this.breadcrumbs = [];
      this.currentChildren = this.tree;
    }
  }

  private findPath(
    nodes: GalleryTreeNode[],
    id: string,
    path: GalleryTreeNode[]
  ): GalleryTreeNode[] | null {
    for (const node of nodes) {
      const nextPath = [...path, node];
      if (node.id === id) return nextPath;
      const found = this.findPath(node.children, id, nextPath);
      if (found) return found;
    }
    return null;
  }
}
