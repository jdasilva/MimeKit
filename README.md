# MimeKit

<a href="https://scan.coverity.com/projects/3201">
  <img alt="Coverity Scan Build Status"
       src="https://scan.coverity.com/projects/3201/badge.svg"/>
</a>

## What is MimeKit?

MimeKit is a C# library which may be used for the creation and parsing of messages using the Multipurpose Internet Mail Extension (MIME), as defined by [numerous IETF specifications](https://github.com/jstedfast/MimeKit/blob/master/RFCs.md).

## History

As a developer and user of email clients, I had come to realize that the vast majority of email client
(and server) software had less-than-satisfactory MIME implementations. More often than not these email clients
created broken MIME messages and/or would incorrectly try to parse a MIME message thus subtracting from the full
benefits that MIME was meant to provide. MimeKit is meant to address this issue by following the MIME specification
as closely as possible while also providing programmers with an extremely easy to use high-level API.

This led me, at first, to implement another MIME parser library called [GMime](http://spruce.sourceforge.net/gmime)
which is implemented in C and later added a C# binding called GMime-Sharp.

Now that I typically find myself working in C# rather than lower level languages like C, I decided to
begin writing a new parser in C# which would not depend on GMime. This would also allow me to have more
flexibility in that I'd be able use Generics and create a more .NET-compliant API.

## Performance

While mainstream beliefs may suggest that C# can never be as fast as C, it turns out that with a bit of creative
parser design and a few clever optimizations 
<sup>[[1](http://jeffreystedfast.blogspot.com/2013/09/optimization-tips-tricks-used-by.html)]
[[2](http://jeffreystedfast.blogspot.com/2013/10/optimization-tips-tricks-used-by.html)]</sup>, MimeKit's performance
is actually [on par with GMime](http://jeffreystedfast.blogspot.com/2014/03/gmime-gets-speed-boost.html).

Since GMime is pretty well-known as a high-performance native MIME parser and MimeKit more-or-less matches GMime's
performance, it stands to reason that MimeKit is likely unsurpassed in performance in the .NET MIME parser space.

For a comparison, as I [blogged here](http://jeffreystedfast.blogspot.com/2013/10/optimization-tips-tricks-used-by.html)
(I have since optimized MimeKit by at least another 30%), MimeKit is more than 25x faster than OpenPOP.NET, 75x faster
than SharpMimeTools, and 65x faster than regex-based parsers. Even the commercial MIME parser offerings such as LimiLabs'
Mail.dll and NewtonIdeas' Mime4Net cannot even come close to matching MimeKit's performance (they are both orders of
magnitude slower than MimeKit).

## License Information

MimeKit is Copyright (C) 2012-2015 Xamarin Inc. and is licensed under the MIT license:

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

## Installing via NuGet

The easiest way to install MimeKit is via [NuGet](https://www.nuget.org/packages/MimeKit/).

In Visual Studio's [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console),
simply enter the following command:

    Install-Package MimeKit

## Building

First, you'll need to clone MimeKit and Bouncy Castle from my GitHub repository:

    git clone https://github.com/jstedfast/MimeKit.git
    git clone https://github.com/jstedfast/bc-csharp.git
    git clone https://github.com/jstedfast/Portable.Text.Encoding.git

Currently, MimeKit depends on the vs2010 branch of bc-csharp for the Visual Studio 2010 project
files that I've added (to replace the Visual Studio 2003 project files). To switch to that branch,

    cd bc-csharp
    git checkout -b vs2010 origin/vs2010

In the top-level MimeKit source directory, there are several solution files:

* MimeKit.sln includes projects for .NET 4.5, .NET 4.0, PCL (Profile7 and Profile111), Xamarin.Android, Xamarin.iOS, and Xamarin.Mac as well as the unit tests.
* MimeKit.Mobile.sln just includes the Xamarin.Android and Xamarin.iOS projects.
* MimeKit.Net40.sln just includes the .NET 4.0 project.

If you don't have the Xamarin products, you'll probably want to open the MimeKit.Net40.sln instead of MimeKit.sln.

Once you've opened the appropriate MimeKit solution file in either Xamarin Studio or Visual Studio 2010+ (either will work),
you can simply choose the Debug or Release build configuration and then build.

Note: The Release build will generate the xml API documentation, but the Debug build will not.

## Using MimeKit

### Parsing Messages

One of the more common operations that MimeKit is meant for is parsing email messages from arbitrary streams.
There are two ways of accomplishing this task.

The first way is to use one of the `Load()` methods on `MimeKit.MimeMessage`:

```csharp
// Load a MimeMessage from a stream
var message = MimeMessage.Load (stream);
```

The second way is to use the `MimeParser` class. For the most part, using the `MimeParser` directly is not necessary
unless you wish to parse a Unix mbox file stream. However, this is how you would do it:

```csharp
// Load a MimeMessage from a stream
var parser = new MimeParser (stream, MimeFormat.Entity);
var message = parser.ParseMessage ();
```

For Unix mbox file streams, you would use the parser like this:

```csharp
// Load every message from a Unix mbox
var parser = new MimeParser (stream, MimeFormat.Mbox);
while (!parser.IsEndOfStream) {
    var message = parser.ParseMessage ();

    // do something with the message
}
```

### Getting the Body of a Message

A common misunderstanding about email is that there is a well-defined message body and then a list
of attachments. This is not really the case. The reality is that MIME is a tree structure of content,
much like a file system.

Luckily, MIME does define a set of general rules for how mail clients should interpret this tree
structure of MIME parts. The `Content-Disposition` header is meant to provide hints to the receiving
client as to which parts are meant to be displayed as part of the message body and which are meant
to be interpreted as attachments.

The `Content-Disposition` header will generally have one of two values: `inline` or `attachment`.

The meaning of these value should be fairly obvious. If the value is `attachment`, then the content
of said MIME part is meant to be presented as a file attachment separate from the core message.
However, if the value is `inline`, then the content of that MIME part is meant to be displayed inline
within the mail client's rendering of the core message body. If the `Content-Disposition` header does
not exist, then it should be treated as if the value were `inline`.

Technically, every part that lacks a `Content-Disposition` header or that is marked as `inline`, then,
is part of the core message body.

There's a bit more to it than that, though.

Modern MIME messages will often contain a `multipart/alternative` MIME container which will generally contain
a `text/plain` and `text/html` version of the text that the sender wrote. The `text/html` version is typically
formatted much closer to what the sender saw in his or her WYSIWYG editor than the `text/plain` version.

The reason for sending the message text in both formats is that not all mail clients are capable of displaying
HTML.

The receiving client should only display one of the alternative views contained within the `multipart/alternative`
container. Since alternative views are listed in order of least faithful to most faithful with what the sender
saw in his or her WYSIWYG editor, the receiving client *should* walk over the list of alternative views starting
at the end and working backwards until it finds a part that it is capable of displaying.

Example:
```
multipart/alternative
  text/plain
  text/html
```

As seen in the example above, the `text/html` part is listed last because it is the most faithful to
what the sender saw in his or her WYSIWYG editor when writing the message.

To make matters even more complicated, sometimes modern mail clients will use a `multipart/related`
MIME container instead of a simple `text/html` part in order to embed images and other content
within the HTML.

Example:
```
multipart/alternative
  text/plain
  multipart/related
    text/html
    image/jpeg
    video/mp4
    image/png
```

In the example above, one of the alternative views is a `multipart/related` container which contains
an HTML version of the message body that references the sibling video and images.

Now that you have a rough idea of how a message is structured and how to interpret various MIME entities,
the next step is learning how to traverse the MIME tree using MimeKit.

Note: For your convenience, MimeKit's `MimeMessage` class has two properties that can help you get the
`text/plain` or `text/html` version of the message body. These are `TextBody` and `HtmlBody`,
respectively.

Keep in mind, however, that at least with the `HtmlBody` property, it may be that the HTML part is
a child of a `multipart/related`, allowing it to refer to images and other types of media that
are also contained within that `multipart/related` entity. This property is really only a convenience
property and is not a really good substitute for traversing the MIME structure yourself so that you
may properly interpret related content.

### Traversing a MimeMessage

The `MimeMessage.Body` is the top-level MIME entity of the message. Generally, it will either be a
`TextPart` or a `Multipart`.

As an example, if you wanted to rip out all of the attachments of a message, your code might look
something like this:

```csharp
var attachments = new List<MimePart> ();
var multiparts = new List<Multipart> ();
var iter = new MimeIterator (message);

// collect our list of attachments and their parent multiparts
while (iter.MoveNext ()) {
    var multipart = iter.Parent as Multipart;
    var part = iter.Current as MimePart;

    if (multipart != null && part != null && part.IsAttachment) {
        // keep track of each attachment's parent multipart
        multiparts.Add (multipart);
        attachments.Add (part);
    }
}

// now remove each attachment from its parent multipart...
for (int i = 0; i < attachments.Count; i++)
    multiparts[i].Remove (attachments[i]);
```

### Quick and Dirty Enumeration of Message Body Parts

If you would rather skip the proper way of traversing a MIME tree, another option that MimeKit provides
is a simple enumerator over the message's body parts in a flat (depth-first) list.

You can access this flat list via the `BodyParts` property, like so:

```csharp
foreach (var part in message.BodyParts) {
   // do something
}
```

Another helper property on the MimeMessage class is the `Attachments` property which works
much the same way as the `BodyParts` property except that it will only contain MIME parts
which have a `Content-Disposition` header value that is set to `attachment`.

### Getting the Decoded Content of a MIME Part

At some point, you're going to want to extract the decoded content of a `MimePart` (such as an image) and
save it to disk or feed it to a UI control to display it.

Once you've found the `MimePart` object that you'd like to extract the content of, here's how you can
save the decoded content to a file:

```csharp
// This will get the name of the file as specified by the sending mail client.
// Note: this value *may* be null, so you'll want to handle that case in your code.
var fileName = part.FileName;

using (var stream = File.Create (fileName)) {
    part.ContentObject.DecodeTo (stream);
}
```

You can also get access to the original raw content by "opening" the `ContentObject`. This might be useful
if you want to pass the content off to a UI control that can do its own loading from a stream.

```csharp
using (var stream = part.ContentObject.Open ()) {
    // At this point, you can now read from the stream as if it were the original,
    // raw content. Assuming you have an image UI control that could load from a
    // stream, you could do something like this:
    imageControl.Load (stream);
}
```

There are a number of useful filters that can be applied to a `FilteredStream`, so if you find this type of
interface appealing, I suggest taking a look at the available filters in the `MimeKit.IO.Filters` namespace
or even write your own! The possibilities are limited only by your imagination.

### Creating a Simple Message

Creating MIME messages using MimeKit is really trivial.

```csharp
var message = new MimeMessage ();
message.From.Add (new MailboxAddress ("Joey", "joey@friends.com"));
message.To.Add (new MailboxAddress ("Alice", "alice@wonderland.com"));
message.Subject = "How you doin?";

message.Body = new TextPart ("plain") {
    Text = @"Hey Alice,

What are you up to this weekend? Monica is throwing one of her parties on
Saturday and I was hoping you could make it.

Will you be my +1?

-- Joey
"
};
```

A `TextPart` is a leaf-node MIME part with a text media-type. The first argument to the `TextPart` constructor
specifies the media-subtype, in this case, "plain". Another media subtype you are probably familiar with
is the "html" subtype. Some other examples include "enriched", "rtf", and "csv".

The `Text` property is the easiest way to both get and set the string content of the MIME part.

### Creating a Message with Attachments

Attachments are just like any other `MimePart`, the only difference is that they typically have
a Content-Disposition header with a value of "attachment" instead of "inline" or no
Content-Disposition header at all.

Typically, when a mail client adds attachments to a message, it will create a multipart/mixed
part and add the text body part and all of the file attachments to the multipart/mixed.

Here's how you can do that with MimeKit:

```csharp
var message = new MimeMessage ();
message.From.Add (new MailboxAddress ("Joey", "joey@friends.com"));
message.To.Add (new MailboxAddress ("Alice", "alice@wonderland.com"));
message.Subject = "How you doin?";

// create our message text, just like before (except don't set it as the message.Body)
var body = new TextPart ("plain") {
    Text = @"Hey Alice,

What are you up to this weekend? Monica is throwing one of her parties on
Saturday and I was hoping you could make it.

Will you be my +1?

-- Joey
"
};

// create an image attachment for the file located at path
var attachment = new MimePart ("image", "gif") {
    ContentObject = new ContentObject (File.OpenRead (path), ContentEncoding.Default),
    ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
    ContentTransferEncoding = ContentEncoding.Base64,
    FileName = Path.GetFileName (path)
};

// now create the multipart/mixed container to hold the message text and the
// image attachment
var multipart = new Multipart ("mixed");
multipart.Add (body);
multipart.Add (attachment);

// now set the multipart/mixed as the message body
message.Body = multipart;
```

Of course, that is just a simple example. A lot of modern mail clients such as Outlook or Thunderbird will 
send out both a text/html and a text/plain version of the message text. To do this, you'd create a `TextPart`
for the text/plain part and a `TextPart` for the text/html part and then add them to a multipart/alternative
like so:

```csharp
var attachment = CreateAttachment ();
var plain = CreateTextPlainPart ();
var html = CreateTextHtmlPart ();

// Note: it is important that the text/html part is added second, because it is the
// most expressive version and (probably) the most faithful to the sender's WYSIWYG 
// editor.
var alternative = new Multipart ("alternative");
alternative.Add (plain);
alternative.Add (html);

// now create the multipart/mixed container to hold the multipart/alternative
// and the image attachment
var multipart = new Multipart ("mixed");
multipart.Add (alternative);
multipart.Add (attachment);

// now set the multipart/mixed as the message body
message.Body = multipart;
```

### Creating a Message Using a BodyBuilder (not Arnold Schwarzenegger)

If you are used to System.Net.Mail's API for creating messages, you will probably find using a `BodyBuilder`
much more friendly than manually creating the tree of MIME parts. Here's how you could create a message body
using a `BodyBuilder`:

```csharp
var message = new MimeMessage ();
message.From.Add (new MailboxAddress ("Joey", "joey@friends.com"));
message.To.Add (new MailboxAddress ("Alice", "alice@wonderland.com"));
message.Subject = "How you doin?";

var builder = new BodyBuilder ();

// Set the plain-text version of the message text
builder.TextBody = @"Hey Alice,

What are you up to this weekend? Monica is throwing one of her parties on
Saturday and I was hoping you could make it.

Will you be my +1?

-- Joey
";

// Set the html version of the message text
builder.HtmlBody = @"<p>Hey Alice,<br>
<p>What are you up to this weekend? Monica is throwing one of her parties on
Saturday and I was hoping you could make it.<br>
<p>Will you be my +1?<br>
<p>-- Joey<br>
<center><img src=""sexy-pose.jpg""></center>";

// Since sexy-pose.jpg is referenced from the html text, we'll need to add it
// to builder.LinkedResources
builder.LinkedResources.Add ("C:\\Users\\Joey\\Documents\\SexySelfies\\sexy-pose.jpg");

// We may also want to attach a calendar event for Monica's party...
builder.Attachments.Add ("C:\\Users\Joey\\Documents\\party.ics");

// Now we just need to set the message body and we're done
message.Body = builder.ToMessageBody ();
```

### Preparing to use MimeKit's S/MIME support

Before you can begin using MimeKit's S/MIME support, you will need to decide which
database to use for certificate storage.

If you are targetting any of the Xamarin platforms (or Linux), you won't need to do
anything (although you certianly can if you want to) because, by default, I've
configured MimeKit to use the Mono.Data.Sqlite binding to SQLite.

If you are, however, on any of the Windows platforms, you'll need to pick a System.Data
provider such as [System.Data.SQLite](https://www.nuget.org/packages/System.Data.SQLite).
Once you've made your choice and installed it (via NuGet or however), you'll need to
implement your own `SecureMimeContext` subclass. Luckily, it's very simple to do. Assuming
you've chosen System.Data.SQLite, here's how you'd implement your own `SecureMimeContext`
class:

```csharp
using System.Data.SQLite;
using MimeKit.Cryptography;

using MyAppNamespace {
    class MySecureMimeContext : DefaultSecureMimeContext
    {
        public MySecureMimeContext () : base (OpenDatabase ("C:\\wherever\\certdb.sqlite"))
        {
        }

        static IX509CertificateDatabase OpenDatabase (string fileName)
        {
            var builder = new SQLiteConnectionStringBuilder ();
            builder.DateTimeFormat = SQLiteDateFormats.Ticks;
            builder.DataSource = fileName;

            if (!File.Exists (fileName))
                SQLiteConnection.CreateFile (fileName);

            var sqlite = new SQLiteConnection (builder.ConnectionString);
            sqlite.Open ();

            return new SqliteCertificateDatabase (sqlite, "password");
        }
    }
}
```

Now that you've implemented your own `SecureMimeContext`, you'll want to register it with MimeKit:

```csharp
CryptographyContext.Register (typeof (MySecureMimeContext));
```

Now you are ready to encrypt, decrypt, sign and verify S/MIME messages!

### Preparing to use MimeKit's PGP/MIME support

Like with S/MIME support, you also need to register your own `OpenPgpContext`. Unlike S/MIME, however,
you don't need to choose a database if you subclass `GnuPGContext` because it uses GnuPG's PGP keyrings
to load and store public and private keys. If you choose to subclass `GnuPGContext`, the only thing you
you need to do is implement a password callback method:

```csharp
using MimeKit.Cryptography;

namespace MyAppNamespace {
    class MyGnuPGContext : GnuPGContext
    {
        public MyGnuPgContext () : base ()
        {
        }

        protected override string GetPasswordForKey (PgpSecretKey key)
        {
            // prompt the user (or a secure password cache) for the password for the specified secret key.
            return "password";
        }
    }
}
```

Once again, to register your `OpenPgpContext`, you can use the following code snippet:

```csharp
CryptographyContext.Register (typeof (MyGnuPGContext));
```

Now you are ready to encrypt, decrypt, sign and verify PGP/MIME messages!

### Encrypting Messages with S/MIME

S/MIME uses an application/pkcs7-mime MIME part to encapsulate encrypted content (as well as other things).

```csharp
var joey = new MailboxAddress ("Joey", "joey@friends.com");
var alice = new MailboxAddress ("Alice", "alice@wonderland.com");

var message = new MimeMessage ();
message.From.Add (joey);
message.To.Add (alice);
message.Subject = "How you doin?";

// create our message body (perhaps a multipart/mixed with the message text and some
// image attachments, for example)
var body = CreateMessageBody ();

// now to encrypt our message body using our custom S/MIME cryptography context
using (var ctx = new MySecureMimeContext ()) {
    // Note: this assumes that "Alice" has an S/MIME certificate with an X.509
    // Subject Email identifier that matches her email address. If she doesn't,
    // try using a SecureMailboxAddress which allows you to specify the
    // fingerprint of her certificate to use for lookups.
    message.Body = ApplicationPkcs7Mime.Encrypt (ctx, message.To.Mailboxes, body);
}
```

### Decrypting S/MIME Messages

As mentioned earlier, S/MIME uses an application/pkcs7-mime part with an "smime-type" parameter with a value of
"enveloped-data" to encapsulate the encrypted content.

The first thing you must do is find the ApplicationPkcs7Mime part (see the section on traversing MIME parts).

```csharp
if (entity is ApplicationPkcs7Mime) {
    var pkcs7 = (ApplicationPkcs7Mime) entity;

    if (pkcs7.SecureMimeType == SecureMimeType.EnvelopedData)
        return pkcs7.Decrypt ();
}
```

### Encrypting Messages with PGP/MIME

Unlike S/MIME, PGP/MIME uses multipart/encrypted to encapsulate its encrypted data.

```csharp
var joey = new MailboxAddress ("Joey", "joey@friends.com");
var alice = new MailboxAddress ("Alice", "alice@wonderland.com");

var message = new MimeMessage ();
message.From.Add (joey);
message.To.Add (alice);
message.Subject = "How you doin?";

// create our message body (perhaps a multipart/mixed with the message text and some
// image attachments, for example)
var body = CreateMessageBody ();

// now to encrypt our message body using our custom PGP/MIME cryptography context
using (var ctx = new MyGnuPGContext ()) {
    // Note: this assumes that "Alice" has a public PGP key that matches her email
    // address. If she doesn't, try using a SecureMailboxAddress which allows you
    // to specify the fingerprint of her public PGP key to use for lookups.
    message.Body = MultipartEncrypted.Encrypt (ctx, message.To.Mailboxes, body);
}
```

### Decrypting PGP/MIME Messages

As mentioned earlier, PGP/MIME uses a multipart/encrypted part to encapsulate the encrypted content.

A multipart/encrtpted contains exactly 2 parts: the first `MimeEntity` is the version information while the
second `MimeEntity` is the actual encrypted content and will typically be an application/octet-stream.

The first thing you must do is find the `MultipartEncrypted` part (see the section on traversing MIME parts).

```csharp
if (entity is MultipartEncrypted) {
    var encrypted = (MultipartEncrypted) entity;

    return encrypted.Decrypt ();
}
```

### Digitally Signing Messages with S/MIME or PGP/MIME

Both S/MIME and PGP/MIME use a multipart/signed to contain the signed content and the detached signature data.

Here's how you might digitally sign a message using S/MIME:

```csharp
var joey = new MailboxAddress ("Joey", "joey@friends.com");
var alice = new MailboxAddress ("Alice", "alice@wonderland.com");

var message = new MimeMessage ();
message.From.Add (joey);
message.To.Add (alice);
message.Subject = "How you doin?";

// create our message body (perhaps a multipart/mixed with the message text and some
// image attachments, for example)
var body = CreateMessageBody ();

// now to digitally sign our message body using our custom S/MIME cryptography context
using (var ctx = new MySecureMimeContext ()) {
    // Note: this assumes that "Joey" has an S/MIME signing certificate and private key
    // with an X.509 Subject Email identifier that matches Joey's email address.
    message.Body = MultipartSigned.Create (ctx, joey, DigestAlgorithm.Sha1, body);
}
```

For S/MIME, if you have a way for the user to configure which S/MIME certificate to use
as their signing certificate, you could also do something more like this:

```csharp
// now to digitally sign our message body using our custom S/MIME cryptography context
using (var ctx = new MySecureMimeContext ()) {
    var certificate = GetJoeysX509Certificate ();
    var signer = new CmsSigner (certificate);
    signer.DigestAlgorithm = DigestAlgorithm.Sha1;

    message.Body = MultipartSigned.Create (ctx, signer, body);
}
```

If you'd prefer to use PGP instead of S/MIME, things work almost exactly the same except that you
would use an OpenPGP cryptography context. For example, you might use a subclass of the
`GnuPGContext` that comes with MimeKit if you want to re-use the user's GnuPG keyrings (you can't
use `GnuPGContext` directly because it has no way of prompting the user for their passphrase).

For the sake of this example, let's pretend that you've written a minimal subclass of
`MimeKit.Cryptography.GnuPGContext` that simply overrides the `GetPassword()` method and
that this subclass is called `MyGnuPGContext`.

```csharp
// now to digitally sign our message body using our custom OpenPGP cryptography context
using (var ctx = new MyGnuPGContext ()) {
    // Note: this assumes that "Joey" has a PGP key that matches his email address.
    message.Body = MultipartSigned.Create (ctx, joey, DigestAlgorithm.Sha1, body);
}
```

Just like S/MIME, however, you can also do your own PGP key lookups instead of
relying on email addresses to match up with the user's private key.

```csharp
// now to digitally sign our message body using our custom OpenPGP cryptography context
using (var ctx = new MyGnuPGContext ()) {
    var key = GetJoeysPrivatePgpKey ();
    message.Body = MultipartSigned.Create (ctx, key, DigestAlgorithm.Sha1, body);
}
```

### Verifying S/MIME and PGP/MIME Digital Signatures

As mentioned earlier, both S/MIME and PGP/MIME typically use a multipart/signed part to contain the
signed content and the detached signature data.

A multipart/signed contains exactly 2 parts: the first `MimeEntity` is the signed content while the second
`MimeEntity` is the detached signature and, by default, will either be an `ApplicationPgpSignature` part or
an `ApplicationPkcs7Signature` part (depending on whether the sending client signed using OpenPGP or S/MIME).

Because the multipart/signed part may have been signed by multiple signers, it is important to
verify each of the digital signatures (one for each signer) that are returned by the
`MultipartSigned.Verify()` method:

```csharp
if (entity is MultipartSigned) {
    var signed = (MultipartSigned) entity;

    foreach (var signature in signed.Verify ()) {
        try {
            bool valid = signature.Verify ();

            // If valid is true, then it signifies that the signed content has not been
            // modified since this particular signer signed the content.
            //
            // However, if it is false, then it indicates that the signed content has
            // been modified.
        } catch (DigitalSignatureVerifyException) {
            // There was an error verifying the signature.
        }
    }
}
```

It should be noted, however, that while most S/MIME clients will use the preferred multipart/signed
approach, it is possible that you may encounter an application/pkcs7-mime part with an "smime-type"
parameter set to "signed-data". Luckily, MimeKit can handle this format as well:

```csharp
if (entity is ApplicationPkcs7Mime) {
    var pkcs7 = (ApplicationPkcs7Mime) entity;

    if (pkcs7.SecureMimeType == SecureMimeType.SignedData) {
        // extract the original content and get a list of signatures
        MimeEntity extracted;

        // Note: if you are rendering the message, you'll want to render the
        // extracted mime part rather than the application/pkcs7-mime part.
        foreach (var signature in pkcs7.Verify (out extracted)) {
            try {
                bool valid = signature.Verify ();

                // If valid is true, then it signifies that the signed content has not
                // been modified since this particular signer signed the content.
                //
                // However, if it is false, then it indicates that the signed content
                // has been modified.
            } catch (DigitalSignatureVerifyException) {
                // There was an error verifying the signature.
            }
        }
    }
}
```

## Contributing

The first thing you'll need to do is fork MimeKit to your own GitHub repository. Once you do that,

    git clone git@github.com/<your-account>/MimeKit.git

If you use [Xamarin Studio](http://xamarin.com/studio) or [MonoDevelop](http://monodevelop.com), all of the
solution files are configured with the coding style used by MimeKit. If you use Visual Studio or some
other editor, please try to maintain the existing coding style as best as you can.

Once you've got some changes that you'd like to submit upstream to the official MimeKit repository,
simply send me a Pull Request and I will try to review your changes in a timely manner.

If you'd like to contribute but don't have any particular features in mind to work on, check out the issue
tracker and look for something that might pique your interest!

## Donate

MimeKit is a personal open source project that I have put thousands of hours into perfecting with the
goal of making it not only the very best MIME parser framework for .NET, but the best MIME parser
framework for any programming language. I need your help to achieve this.

<a href="http://www.pledgie.com/campaigns/29300" target="_blank">
  <img src="http://www.pledgie.com/campaigns/29300.png?skin_name=chrome"
       alt="Click here to lend your support to MimeKit and MailKit by making a donation via pledgie.com!"
       border="0" />
</a>

## Reporting Bugs

Have a bug or a feature request? [Please open a new issue](https://github.com/jstedfast/MimeKit/issues).

Before opening a new issue, please search for existing issues to avoid submitting duplicates.

## Documentation

API documentation can be found at [http://mimekit.net/docs](http://mimekit.net/docs).
