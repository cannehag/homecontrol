using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers
{
    // Tesla's Fleet API looks for the app's public key at this fixed address on
    // the app's domain - once when the app is registered, and the car fetches it
    // when the key is added as a virtual key. The private half is
    // `tesla-private-key` in the secrets. Public, so no [Authorize].
    public class TeslaKeyController : Controller
    {
        private const string PublicKey =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE1l8zPVeC4uE912GmXMdvVRzYlYmZ\n" +
            "H9WsWdzjUjfZbO3NdEeoPKnz974nT5tEGVZUB6KvHULRwPOiZfotNG6YYQ==\n" +
            "-----END PUBLIC KEY-----\n";

        [HttpGet("/.well-known/appspecific/com.tesla.3p.public-key.pem")]
        public IActionResult PublicKeyPem() => Content(PublicKey, "application/x-pem-file");
    }
}
