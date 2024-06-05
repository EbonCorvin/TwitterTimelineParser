# TwitterTimelineParser

## Description

A .NET library that fetch the user's like timeline from Twitter's graphql API, and convert them to a bunch of ``Tweet`` objects that contain the contet, attached image and video, post date and such. The screenshot below is the example of an returned object.
![image](https://github.com/EbonCorvin/TwitterTimelineParser/assets/153107703/08c40d71-aca0-4fb9-a62a-022a516227fd)

The usage of this library? You can have a look at the sample folder, it contains 2 possible use cases of the library: 
1. Download every media you "liked" on Twitter, or;
2. Forward every "liked" tweet to a Telegram channel.

Just be creative and you should be able to make it a even better use :)

Please note that this library use Regex to parse the content of the returned JSON. It provides a bit better performance than parsing it as a whole JSON object. However, a sightly bit change to the JSON format may make the library unusable.

I planned to rewrite it with some JSON library in the future.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Installation

Just add the source code of the library, and use the library as one of the reference of your project should do the job. Or just refer the built .DLL file in your project.

## Usage

1. refer the library in your project by following the [Installation](#installation) section.
2. Create a ``TwitterLikeParser`` object, remember to supply the necessary values to the constructor:
    1. ``twitterId`` - The twitter ID (the number) of someone, not to confuse with the handle of a Twitter user
    2. ``cookie`` - The cookie on the Twitter website. You should be able to get it by using F12 Devtool of any browser. It's how this library doesn't require user to login
    3. ``csrt`` - The CSRT token of the user, you can also get it by using F12 Devtool of any browser.
3. Call ``NextPage()`` to start getting the first page of the timeline. If the values supplied to the constructor are correct, it should return an array of ``Tweet`` object.
4. Keep calling ``NextPage()`` to get the second page and so on, until it return ``false``, indicating that no more page is available.

A simple example call would be:

```C#
TwitterLikeParser parser = new TwitterLikeParser(twitter_id, cookie, csrt);
while (parser.NextPage())
{
    foreach (Tweet tweet in parser.Tweets)
    {
        // Have fun with the Tweet object
    }
}
```

## ``Tweet`` and ``Media`` objects

The ``Tweet`` and ``Media`` objects contains the following fields:

```C#
public class Tweet
{
    public String Author { get; set; }
    public String TweetId { get; set; }
    public String Content { get; set; }
    public String CreateDate { get; set; }
    public String TweetUrl { get; set; }
    public Media[] Medias { get; set; }
    public String MediaJoined { get; set; }
}

public class Media
{
    // "photo" and "video"
    public String MediaType { get; set; }
    public String Url { get; set; }
}
```

## License

[License](License)
