using System;
using System.Collections.Generic;

namespace Site.Models.Gallery
{
    public class GalleryTreeNode
    {
        public string Id { get; set; }

        // "folder" | "album"
        public string Type { get; set; }

        public string Name { get; set; }

        public string CoverUrl { get; set; }

        // For an album: count of its own images. For a folder: sum across all
        // descendant albums.
        public int ImageCount { get; set; }

        public List<GalleryTreeNode> Children { get; set; } = new();
    }

    public class GalleryStatusDto
    {
        public bool Connected { get; set; }

        public int LightroomAuthExpiresInDays { get; set; }
    }

    public class AlbumDetailDto
    {
        public string AlbumId { get; set; }

        public string AlbumName { get; set; }

        // Ancestor folders, root first, for rendering a breadcrumb trail back to the
        // gallery home instead of a plain "back" button - lets you jump to any
        // ancestor directly when nested several folders deep.
        public List<BreadcrumbItemDto> Breadcrumbs { get; set; } = new();

        public List<AlbumImageDto> Images { get; set; } = new();
    }

    public class BreadcrumbItemDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class AlbumImageDto
    {
        public string Id { get; set; }

        public string ThumbnailUrl { get; set; }

        public string FullUrl { get; set; }

        // Pixel dimensions of the image behind FullUrl - needed by the frontend's
        // PhotoSwipe lightbox to size slides without distorting the image.
        public int? Width { get; set; }

        public int? Height { get; set; }

        public PhotoMetadataDto Metadata { get; set; }
    }

    public class PhotoMetadataDto
    {
        public string FileName { get; set; }

        public DateTimeOffset? CaptureDate { get; set; }

        public string CameraMake { get; set; }

        public string CameraModel { get; set; }

        public string Lens { get; set; }

        // e.g. "f/5.0"
        public string Aperture { get; set; }

        // e.g. "1/200s"
        public string ShutterSpeed { get; set; }

        public int? Iso { get; set; }

        // e.g. "40mm"
        public string FocalLength { get; set; }

        public bool? FlashFired { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Rights { get; set; }

        public List<string> Creators { get; set; } = new();

        public List<string> Keywords { get; set; } = new();
    }
}
