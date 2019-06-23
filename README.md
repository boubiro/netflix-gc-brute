# Netflix Giftcard BruteForcer

_(This has been patched a few weeks ago, just posting for people needing help with how to make a cracker/bruteforcer)_

Made this for someone 1-2 months ago, (was) working without a captcha solution with a solid 8K CPM, just needed codes and decent proxies.

The program was sending 5 requests per code :
  - The first one was starting sign up process and fetching cookies/csrf token
  - The second one was selecting plan
  - The third one was making an account with a random 16 chars email and a random password
  - The fourth one was redeeming the code
  - The fifth one was only sent if the 4th request was successful, used to fetch the code balance
  
There's almost everything, just removed authentication related classes.