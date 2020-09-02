//
//  MIT License
//
//  Copyright (c) 2019 Daniel Lupiañez Casares
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
//

#import "NativeMessageHandler.h"

typedef void (*NativeMessageHandlerDelegate)(uint requestId,  const char* payload);

@interface NativeMessageHandler ()
@property (nonatomic, assign) NativeMessageHandlerDelegate mainCallback;
@property (nonatomic, weak) NSOperationQueue *callingOperationQueue;
@end

@implementation NativeMessageHandler

+ (instancetype) defaultHandler
{
    static NativeMessageHandler *_defaultHandler = nil;
    static dispatch_once_t defaultHandlerInitialization;
    
    dispatch_once(&defaultHandlerInitialization, ^{
        _defaultHandler = [[NativeMessageHandler alloc] init];
    });
    
    return _defaultHandler;
}

- (void) sendNativeMessageForDictionary:(NSDictionary *)payloadDictionary forRequestId:(uint)requestId
{
    NSError *error = nil;
    NSData *payloadData = [NSJSONSerialization dataWithJSONObject:payloadDictionary options:0 error:&error];
    NSString *payloadString = error ? [NSString stringWithFormat:@"Serialization error %@", [error localizedDescription]] : [[NSString alloc] initWithData:payloadData encoding:NSUTF8StringEncoding];
    [self sendNativeMessageForString:payloadString forRequestId:requestId];
}

- (void) sendNativeMessageForString:(NSString *)payloadString forRequestId:(uint)requestId
{
    if ([self mainCallback] == NULL)
        return;
    
    if ([self callingOperationQueue])
    {
        [[self callingOperationQueue] addOperationWithBlock:^{
            [self mainCallback](requestId, [payloadString UTF8String]);
        }];
    }
    else
    {
        [self mainCallback](requestId, [payloadString UTF8String]);
    }
    
}

@end

void AppleAuth_IOS_SetupNativeMessageHandlerCallback(NativeMessageHandlerDelegate callback)
{
    [[NativeMessageHandler defaultHandler] setMainCallback:callback];
    [[NativeMessageHandler defaultHandler] setCallingOperationQueue: [NSOperationQueue currentQueue]];
}
