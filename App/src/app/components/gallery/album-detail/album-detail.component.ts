import { Location } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import PhotoSwipeLightbox from 'photoswipe/lightbox';
import { AlbumDetail, AlbumImage, GalleryService } from '../gallery.service';

@Component({
  selector: 'home-album-detail',
  templateUrl: './album-detail.component.html',
  styleUrls: ['./album-detail.component.less'],
})
export class AlbumDetailComponent implements OnInit, OnDestroy {
  loading = true;
  album: AlbumDetail | null = null;

  private lightbox: PhotoSwipeLightbox | null = null;

  constructor(
    private route: ActivatedRoute,
    private location: Location,
    private galleryService: GalleryService
  ) {}

  goBack() {
    this.location.back();
  }

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const albumId = params.get('albumId');
      if (!albumId) return;

      this.loading = true;
      this.destroyLightbox();

      this.galleryService.getAlbum(albumId).subscribe((album) => {
        this.album = album;
        this.loading = false;
        setTimeout(() => this.initLightbox());
      });
    });
  }

  ngOnDestroy() {
    this.destroyLightbox();
  }

  captionFor(image: AlbumImage): string {
    const m = image.metadata;
    const parts = [
      [m.cameraMake, m.cameraModel].filter(Boolean).join(' '),
      m.lens,
      [m.aperture, m.shutterSpeed, m.iso ? `ISO ${m.iso}` : null, m.focalLength]
        .filter(Boolean)
        .join(' · '),
      m.captureDate ? new Date(m.captureDate).toLocaleDateString('sv-SE') : null,
      [m.city, m.state, m.country].filter(Boolean).join(', '),
      m.keywords.length ? m.keywords.join(', ') : null,
    ].filter(Boolean);

    return parts.join('\n');
  }

  private initLightbox() {
    this.lightbox = new PhotoSwipeLightbox({
      gallery: '#album-grid',
      children: 'a',
      pswpModule: () => import('photoswipe'),
    });

    this.lightbox.on('uiRegister', () => {
      this.lightbox!.pswp!.ui!.registerElement({
        name: 'custom-caption',
        order: 9,
        isButton: false,
        appendTo: 'root',
        onInit: (el) => {
          el.className = 'pswp__custom-caption';
          this.lightbox!.pswp!.on('change', () => {
            const currSlideElement = this.lightbox!.pswp!.currSlide?.data.element as HTMLElement | undefined;
            el.textContent = currSlideElement?.dataset['caption'] ?? '';
          });
        },
      });
    });

    this.lightbox.init();
  }

  private destroyLightbox() {
    this.lightbox?.destroy();
    this.lightbox = null;
  }
}
