﻿//
// UuEncoder.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2012 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

namespace MimeKit {
	public class UuEncoder : IMimeEncoder
	{
		const int MaxInputPerLine = 45;
		const int MaxOutputPerLine = ((MaxInputPerLine / 3) * 4) + 2;

		byte[] uubuf = new byte[60];
		uint saved;
		byte nsaved;
		byte uulen;

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeKit.UuEncoder"/> class.
		/// </summary>
		public UuEncoder ()
		{
			Reset ();
		}

		/// <summary>
		/// Clones the encoder.
		/// </summary>
		public object Clone ()
		{
			var encoder = (UuEncoder) MemberwiseClone ();
			encoder.uubuf = new byte[uubuf.Length];
			Array.Copy (uubuf, encoder.uubuf, uubuf.Length);
			return encoder;
		}

		/// <summary>
		/// Gets the encoding.
		/// </summary>
		/// <value>
		/// The encoding.
		/// </value>
		public ContentEncoding Encoding
		{
			get { return ContentEncoding.UuEncode; }
		}

		/// <summary>
		/// Estimates the length of the output.
		/// </summary>
		/// <returns>
		/// The estimated output length.
		/// </returns>
		/// <param name='inputLength'>
		/// The input length.
		/// </param>
		public int EstimateOutputLength (int inputLength)
		{
			return (((inputLength + 2) / MaxInputPerLine) * MaxOutputPerLine) + MaxOutputPerLine + 2;
		}

		void ValidateArguments (byte[] input, int startIndex, int length, byte[] output)
		{
			if (input == null)
				throw new ArgumentNullException ("input");

			if (startIndex < 0 || startIndex > input.Length)
				throw new ArgumentOutOfRangeException ("startIndex");

			if (length < 0 || startIndex + length > input.Length)
				throw new ArgumentOutOfRangeException ("length");

			if (output == null)
				throw new ArgumentNullException ("output");

			if (output.Length < EstimateOutputLength (length))
				throw new ArgumentException ("The output buffer is not large enough to contain the encoded input.", "output");
		}

		static byte Encode (int c)
		{
			return c != 0 ? (byte) (c + 0x20) : (byte) '`';
		}

		unsafe int UnsafeEncode (byte* input, int length, byte[] outbuf, byte* output, byte *uuptr)
		{
			if (length == 0)
				return 0;
			
			byte* inend = input + length;
			byte* outptr = output;
			byte* inptr = input;
			byte* bufptr;
			byte b0, b1, b2;
			
			if ((length + uulen) < 45) {
				// not enough input to write a full uuencoded line
				bufptr = uuptr + ((uulen / 3) * 4);
			} else {
				bufptr = outptr + 1;
				
				if (uulen > 0) {
					// copy the previous call's uubuf to output
					Array.Copy (uubuf, 0, outbuf, (int) (bufptr - outptr), ((uulen / 3) * 4));
					bufptr += ((uulen / 3) * 4);
				}
			}
			
			if (nsaved == 2) {
				b0 = (byte) ((saved >> 8) & 0xFF);
				b1 = (byte) (saved & 0xFF);
				b2 = *inptr++;
				nsaved = 0;
				saved = 0;

				// convert 3 input bytes into 4 uuencoded bytes
				*bufptr++ = Encode ((b0 >> 2) & 0x3F);
				*bufptr++ = Encode (((b0 << 4) | ((b1 >> 4) & 0x0F)) & 0x3F);
				*bufptr++ = Encode (((b1 << 2) | ((b2 >> 6) & 0x03)) & 0x3F);
				*bufptr++ = Encode (b2 & 0x3F);

				uulen += 3;
			} else if (nsaved == 1) {
				if ((inptr + 2) < inend) {
					b0 = (byte) (saved & 0xFF);
					b1 = *inptr++;
					b2 = *inptr++;
					nsaved = 0;
					saved = 0;

					// convert 3 input bytes into 4 uuencoded bytes
					*bufptr++ = Encode ((b0 >> 2) & 0x3F);
					*bufptr++ = Encode (((b0 << 4) | ((b1 >> 4) & 0x0F)) & 0x3F);
					*bufptr++ = Encode (((b1 << 2) | ((b2 >> 6) & 0x03)) & 0x3F);
					*bufptr++ = Encode (b2 & 0x3F);

					uulen += 3;
				} else {
					while (inptr < inend) {
						saved = (saved << 8) | *inptr++;
						nsaved++;
					}
				}
			}
			
			while (inptr < inend) {
				while (uulen < 45 && (inptr + 3) <= inend) {
					b0 = *inptr++;
					b1 = *inptr++;
					b2 = *inptr++;

					// convert 3 input bytes into 4 uuencoded bytes
					*bufptr++ = Encode ((b0 >> 2) & 0x3F);
					*bufptr++ = Encode (((b0 << 4) | ((b1 >> 4) & 0x0F)) & 0x3F);
					*bufptr++ = Encode (((b1 << 2) | ((b2 >> 6) & 0x03)) & 0x3F);
					*bufptr++ = Encode (b2 & 0x3F);

					uulen += 3;
				}

				if (uulen >= 45) {
					// output the uu line length
					*outptr = Encode (uulen);
					outptr += ((uulen / 3) * 4) + 1;
					*outptr++ = (byte) '\n';
					uulen = 0;

					if ((inptr + 45) <= inend) {
						// we have enough input to output another full line
						bufptr = outptr + 1;
					} else {
						bufptr = uuptr;
					}
				} else {
					// not enough input to continue...
					for (nsaved = 0, saved = 0; inptr < inend; nsaved++)
						saved = (saved << 8) | *inptr++;
				}
			}

			return (int) (outptr - output);
		}

		/// <summary>
		/// Encodes the specified input into the output buffer.
		/// </summary>
		/// <returns>
		/// The number of bytes written to the output buffer.
		/// </returns>
		/// <param name='input'>
		/// The input buffer.
		/// </param>
		/// <param name='startIndex'>
		/// The starting index of the input buffer.
		/// </param>
		/// <param name='length'>
		/// The length of the input buffer.
		/// </param>
		/// <param name='output'>
		/// The output buffer.
		/// </param>
		public int Encode (byte[] input, int startIndex, int length, byte[] output)
		{
			ValidateArguments (input, startIndex, length, output);

			unsafe {
				fixed (byte* inptr = input, outptr = output, uuptr = uubuf) {
					return UnsafeEncode (inptr + startIndex, length, output, outptr, uuptr);
				}
			}
		}

		unsafe int UnsafeFlush (byte* input, int length, byte[] outbuf, byte* output, byte* uuptr)
		{
			byte* outptr = output;

			if (length > 0)
				outptr += UnsafeEncode (input, length, outbuf, output, uuptr);

			byte* bufptr = uuptr + ((uulen / 3) * 4);
			byte uufill = 0;

			if (nsaved > 0) {
				while (nsaved < 3) {
					saved <<= 8;
					uufill++;
					nsaved++;
				}
				
				if (nsaved == 3) {
					// convert 3 input bytes into 4 uuencoded bytes
					byte b0, b1, b2;
					
					b0 = (byte) ((saved >> 16) & 0xFF);
					b1 = (byte) ((saved >> 8) & 0xFF);
					b2 = (byte) (saved & 0xFF);
					
					*bufptr++ = Encode ((b0 >> 2) & 0x3F);
					*bufptr++ = Encode (((b0 << 4) | ((b1 >> 4) & 0x0F)) & 0x3F);
					*bufptr++ = Encode (((b1 << 2) | ((b2 >> 6) & 0x03)) & 0x3F);
					*bufptr++ = Encode (b2 & 0x3F);
					
					uulen += 3;
					nsaved = 0;
					saved = 0;
				}
			}
			
			if (uulen > 0) {
				int copylen = ((uulen / 3) * 4);
				
				*outptr++ = Encode ((uulen - uufill) & 0xFF);
				Array.Copy (uubuf, 0, outbuf, (int) (outptr - output), copylen);
				outptr += copylen;

				*outptr++ = (byte) '\n';
				uulen = 0;
			}
			
			*outptr++ = Encode (uulen & 0xff);
			*outptr++ = (byte) '\n';

			Reset ();

			return (int) (outptr - output);
		}

		/// <summary>
		/// Encodes the specified input into the output buffer, flushing any internal buffer state as well.
		/// </summary>
		/// <returns>
		/// The number of bytes written to the output buffer.
		/// </returns>
		/// <param name='input'>
		/// The input buffer.
		/// </param>
		/// <param name='startIndex'>
		/// The starting index of the input buffer.
		/// </param>
		/// <param name='length'>
		/// The length of the input buffer.
		/// </param>
		/// <param name='output'>
		/// The output buffer.
		/// </param>
		public int Flush (byte[] input, int startIndex, int length, byte[] output)
		{
			ValidateArguments (input, startIndex, length, output);

			unsafe {
				fixed (byte* inptr = input, outptr = output, uuptr = uubuf) {
					return UnsafeFlush (inptr + startIndex, length, output, outptr, uuptr);
				}
			}
		}

		/// <summary>
		/// Resets the encoder.
		/// </summary>
		public void Reset ()
		{
			nsaved = 0;
			saved = 0;
			uulen = 0;
		}
	}
}