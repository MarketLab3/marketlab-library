from setuptools import setup
from sys import version_info

setup(name='marketlab',
      version='0.0.1',
      description='Marketlab library to replay historic',
      url='https://marketlab.app',
      author='MarketLab, Charles D.',
      author_email='contact@marketlab.app',
      license='MIT',
      packages=['marketlab'],
      install_requires=['pandas', 'requests'],
      keywords='marketlab backtest cryptocurrency cryptocurrencies api bitcoin'
      )